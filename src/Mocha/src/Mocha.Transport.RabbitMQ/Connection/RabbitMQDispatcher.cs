using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Manages the RabbitMQ connection and bounded channel pool used to publish messages.
/// </summary>
public sealed class RabbitMQDispatcher(
    ILogger<RabbitMQDispatcher> logger,
    Func<CancellationToken, ValueTask<IConnection>> connectionFactory,
    Func<IConnection, CancellationToken, Task> onConnectionEstablished)
    : RabbitMQConnectionManagerBase(logger, connectionFactory)
{
    private const int MaxConcurrentPublishes = 100;

    private static readonly CreateChannelOptions s_publishChannelOptions = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true,
        outstandingPublisherConfirmationsRateLimiter: null);

    private readonly object _channelSync = new();
    private readonly SemaphoreSlim _publishSlots = new(MaxConcurrentPublishes);
    private readonly CancellationTokenSource _channelCts = new();
    private readonly Stack<PublisherChannelSlot> _availableSlots = new(MaxConcurrentPublishes);
    private readonly List<PublisherChannelSlot> _slots = new(MaxConcurrentPublishes);
    private readonly Dictionary<IChannel, PublisherChannelSlot> _rentedChannels = new(MaxConcurrentPublishes);

    /// <summary>
    /// Gets a value indicating whether the dispatcher was disposed, after which no publish can succeed.
    /// </summary>
    internal bool IsShutDown => IsDisposed;

    /// <summary>
    /// Gets or sets how long opening a publisher channel may take before its connection is aborted.
    /// </summary>
    internal TimeSpan ChannelCreationTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Rents a channel exclusively until it is returned, waiting when all publisher slots are in use.
    /// The caller must return the channel via ReturnChannelAsync or DiscardChannel.
    /// </summary>
    public async ValueTask<IChannel> RentChannelAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_publishSlots.Wait(0))
        {
            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _channelCts.Token);
            await _publishSlots.WaitAsync(waitCts.Token);
        }

        PublisherChannelSlot? slot = null;
        try
        {
            while (true)
            {
                var connection = await GetConnectionAsync(cancellationToken);
                PublisherChannel owner;
                lock (_channelSync)
                {
                    ObjectDisposedException.ThrowIf(IsDisposed, this);
                    if (!ReferenceEquals(connection, CurrentConnection))
                    {
                        continue;
                    }

                    if (slot is null)
                    {
                        if (!_availableSlots.TryPop(out slot))
                        {
                            slot = new PublisherChannelSlot();
                            _slots.Add(slot);
                        }
                    }

                    if (slot.Current is { } previous
                        && (!ReferenceEquals(previous.Connection, connection)
                            || previous.Channel.Task.IsFaulted
                            || (previous.Channel.Task.IsCompletedSuccessfully && !previous.Channel.Task.Result.IsOpen)))
                    {
                        RetireChannel(slot);
                    }

                    if (slot.Current is null)
                    {
                        slot.Current = new PublisherChannel(connection);
                        slot.Current.Creation = CreateChannelAsync(slot.Current, slot.Cleanup, _channelCts.Token);
                    }

                    owner = slot.Current;
                }

                // Caller cancellation returns the slot; its channel creation remains owned by the dispatcher.
                var channel = await owner.Channel.Task.WaitAsync(cancellationToken);
                lock (_channelSync)
                {
                    ObjectDisposedException.ThrowIf(IsDisposed, this);
                    if (!ReferenceEquals(slot.Current, owner)
                        || !ReferenceEquals(connection, CurrentConnection)
                        || !channel.IsOpen)
                    {
                        continue;
                    }

                    _rentedChannels.Add(channel, slot);
                    return channel;
                }
            }
        }
        catch
        {
            if (slot is not null)
            {
                lock (_channelSync)
                {
                    _availableSlots.Push(slot);
                }
            }

            _publishSlots.Release();
            throw;
        }
    }

    /// <summary>
    /// Returns a rented channel to the pool, or retires it when it is closed or belongs to a lost connection.
    /// </summary>
    public ValueTask ReturnChannelAsync(IChannel channel)
    {
        ReturnChannel(channel, discard: false);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Returns a publisher slot and retires the channel on which a protocol operation failed.
    /// </summary>
    internal void DiscardChannel(IChannel channel) => ReturnChannel(channel, discard: true);

    private void ReturnChannel(IChannel channel, bool discard)
    {
        lock (_channelSync)
        {
            if (!_rentedChannels.Remove(channel, out var slot))
            {
                return;
            }

            if (discard || IsDisposed || !channel.IsOpen
                || !ReferenceEquals(slot.Current?.Connection, CurrentConnection))
            {
                RetireChannel(slot);
            }

            _availableSlots.Push(slot);
        }

        _publishSlots.Release();
    }

    private static void RetireChannel(PublisherChannelSlot slot)
    {
        if (slot.Current is { } channel)
        {
            slot.Current = null;
            slot.Cleanup = DisposePublisherChannelAsync(channel);
        }
    }

    private async Task CreateChannelAsync(
        PublisherChannel owner,
        Task cleanup,
        CancellationToken cancellationToken)
    {
        await Task.Yield();
        using var creationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        creationCts.CancelAfter(ChannelCreationTimeout);
        Task<IChannel>? opening = null;
        try
        {
            await cleanup.WaitAsync(creationCts.Token);
            opening = owner.Connection.CreateChannelAsync(s_publishChannelOptions, creationCts.Token);
            var channel = await opening.WaitAsync(creationCts.Token);
            Logger.CreatedNewChannel();
            owner.Channel.TrySetResult(channel);
        }
        catch (Exception ex)
        {
            owner.Channel.TrySetException(ex);
            _ = owner.Channel.Task.Exception;

            // Close the connection when opening a publisher channel fails.
            try
            {
                await owner.Connection.AbortAsync();
            }
            catch
            {
                // The connection is already closing.
            }

            if (opening is not null)
            {
                try
                {
                    await DisposeChannelSafelyAsync(await opening);
                }
                catch
                {
                    // Opening the channel failed.
                }
            }

            StartReconnecting();
        }
    }

    internal void LogPublishRetry(Exception exception, int attempt, TimeSpan delay)
        => Logger.PublishFailedRetrying(exception, attempt, delay);

    internal void LogReturnedQueueRedeclared(string queueName)
        => Logger.RedeclaredQueueOfReturnedMessage(queueName);

    protected override async Task OnConnectionEstablished(IConnection connection, CancellationToken cancellationToken)
    {
        await onConnectionEstablished(connection, cancellationToken);
    }

    protected override async Task OnBeforeConnectionCreatedAsync(CancellationToken cancellationToken)
    {
        await ClearChannelsAsync();
    }

    protected override async Task OnConnectionLostAsync()
    {
        await ClearChannelsAsync();
        StartReconnecting();
    }

    private async Task ClearChannelsAsync()
    {
        Task[] cleanup;
        lock (_channelSync)
        {
            cleanup = new Task[_slots.Count];
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Current is { } current
                    && (IsDisposed || !current.Connection.IsOpen
                        || !ReferenceEquals(current.Connection, CurrentConnection)))
                {
                    RetireChannel(slot);
                }

                cleanup[i] = slot.Cleanup;
            }
        }

        await Task.WhenAll(cleanup);
    }

    private static async Task DisposePublisherChannelAsync(PublisherChannel channel)
    {
        await channel.Creation;
        if (channel.Channel.Task.IsCompletedSuccessfully)
        {
            await DisposeChannelSafelyAsync(channel.Channel.Task.Result);
        }
    }

    private static async ValueTask DisposeChannelSafelyAsync(IChannel channel)
    {
        try
        {
            if (channel.IsOpen)
            {
                await channel.CloseAsync();
            }
        }
        catch
        {
            // Ignore close errors.
        }
        finally
        {
            try
            {
                await channel.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors.
            }
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await _channelCts.CancelAsync();
        await ClearChannelsAsync();
        _channelCts.Dispose();
    }

    private sealed class PublisherChannelSlot
    {
        public PublisherChannel? Current { get; set; }

        public Task Cleanup { get; set; } = Task.CompletedTask;
    }

    private sealed class PublisherChannel(IConnection connection)
    {
        public IConnection Connection { get; } = connection;

        public TaskCompletionSource<IChannel> Channel { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Creation { get; set; } = Task.CompletedTask;
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Debug, "Created new channel")]
    public static partial void CreatedNewChannel(this ILogger logger);

    [LoggerMessage(
        LogLevel.Warning,
        "Publish attempt {Attempt} failed because the connection to the broker was lost. Retrying in {Delay}")]
    public static partial void PublishFailedRetrying(this ILogger logger, Exception ex, int attempt, TimeSpan delay);

    [LoggerMessage(
        LogLevel.Warning,
        "The broker returned a message because queue {Queue} does not exist. The queue was declared again")]
    public static partial void RedeclaredQueueOfReturnedMessage(this ILogger logger, string queue);
}
