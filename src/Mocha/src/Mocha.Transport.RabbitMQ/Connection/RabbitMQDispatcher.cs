using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Manages the RabbitMQ connection and shared channel used to publish messages.
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
    private PublisherChannel? _channel;
    private Task _channelCleanup = Task.CompletedTask;

    /// <summary>
    /// Gets a value indicating whether the dispatcher was disposed, after which no publish can succeed.
    /// </summary>
    internal bool IsShutDown => IsDisposed;

    /// <summary>
    /// Gets or sets how long opening a publisher channel may take before its connection is aborted.
    /// </summary>
    internal TimeSpan ChannelCreationTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Rents the shared publisher channel, waiting while the maximum number of publishes is in flight.
    /// The caller must return the channel via ReturnChannelAsync or DiscardChannel.
    /// </summary>
    public async ValueTask<IChannel> RentChannelAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        await _publishSlots.WaitAsync(cancellationToken);
        try
        {
            while (true)
            {
                var connection = await GetConnectionAsync(cancellationToken);
                Task<IChannel> channelTask;
                lock (_channelSync)
                {
                    ObjectDisposedException.ThrowIf(IsDisposed, this);
                    if (!ReferenceEquals(connection, CurrentConnection))
                    {
                        continue;
                    }

                    if (_channel is { Channel.Task.IsCompletedSuccessfully: true } previous
                        && !previous.Channel.Task.Result.IsOpen)
                    {
                        _channelCleanup = DisposePublisherChannelAsync(previous);
                        _channel = null;
                    }

                    if (_channel is null)
                    {
                        _channel = new PublisherChannel(connection);
                        _channel.Creation = CreateChannelAsync(_channel, _channelCleanup, _channelCts.Token);
                    }

                    channelTask = _channel.Channel.Task;
                }

                // Cancelling a publish ends only its wait. The dispatcher retains the channel being opened.
                return await channelTask.WaitAsync(cancellationToken);
            }
        }
        catch
        {
            _publishSlots.Release();
            throw;
        }
    }

    /// <summary>
    /// Returns a publish slot while retaining an open channel for subsequent publishes.
    /// </summary>
    public ValueTask ReturnChannelAsync(IChannel channel)
    {
        if (IsDisposed)
        {
            _publishSlots.Release();
            return DisposeChannelSafelyAsync(channel);
        }

        _publishSlots.Release();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Returns a publish slot and retires a channel on which a protocol operation failed.
    /// </summary>
    internal void DiscardChannel(IChannel channel)
    {
        lock (_channelSync)
        {
            if (_channel is { Channel.Task.IsCompletedSuccessfully: true } current
                && ReferenceEquals(current.Channel.Task.Result, channel))
            {
                _channel = null;
                _channelCleanup = DisposePublisherChannelAsync(current);
            }
        }

        _publishSlots.Release();
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
        await ClearChannelAsync();
    }

    protected override async Task OnConnectionLostAsync()
    {
        await ClearChannelAsync();
        StartReconnecting();
    }

    private async Task ClearChannelAsync()
    {
        PublisherChannel? channel;
        Task cleanup;
        lock (_channelSync)
        {
            if (!IsDisposed
                && _channel is { Connection.IsOpen: true } current
                && ReferenceEquals(current.Connection, CurrentConnection))
            {
                return;
            }

            channel = _channel;
            cleanup = _channelCleanup;
            _channel = null;
            _channelCleanup = Task.CompletedTask;
        }

        await cleanup;
        if (channel is not null)
        {
            await DisposePublisherChannelAsync(channel);
        }
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
        await ClearChannelAsync();
        _channelCts.Dispose();
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
