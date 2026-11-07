using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

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
    private const int MaxConcurrentPublishes = 1024;
    private const int MaxPublisherChannels = 100;

    private static readonly CreateChannelOptions s_publishChannelOptions = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true,
        outstandingPublisherConfirmationsRateLimiter: null);

    private readonly object _channelSync = new();
    private readonly SemaphoreSlim _publishSlots = new(MaxConcurrentPublishes);
    private readonly CancellationTokenSource _channelCts = new();
    private readonly Dictionary<IChannel, PublisherChannel> _rentedChannels = new(MaxPublisherChannels);
    private PublisherChannelPool? _channelPool;

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
    public ValueTask<IChannel> RentChannelAsync(CancellationToken cancellationToken = default)
        => RentChannelCoreAsync(null, cancellationToken);

    /// <summary>
    /// Rents a channel shared by concurrent publishes from the same publisher.
    /// The caller must return the channel via ReturnChannelAsync or DiscardChannel.
    /// </summary>
    internal ValueTask<IChannel> RentChannelAsync(object publisher, CancellationToken cancellationToken)
        => RentChannelCoreAsync(publisher, cancellationToken);

    private async ValueTask<IChannel> RentChannelCoreAsync(object? publisher, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_publishSlots.Wait(0))
        {
            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _channelCts.Token);
            await _publishSlots.WaitAsync(waitCts.Token);
        }

        try
        {
            IChannel? channel = null;
            while (channel is null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var connection = await GetConnectionAsync(cancellationToken);
                PublisherChannelPool pool;
                lock (_channelSync)
                {
                    ObjectDisposedException.ThrowIf(IsDisposed, this);
                    if (!ReferenceEquals(connection, CurrentConnection))
                    {
                        continue;
                    }

                    pool = _channelPool ??= new PublisherChannelPool(connection);
                }

                if (!ReferenceEquals(pool.Connection, connection))
                {
                    await ClearChannelsAsync(preserveCurrentConnection: true);
                    continue;
                }

                channel = await RentChannelAsync(pool, publisher, cancellationToken);
            }

            return channel;
        }
        catch
        {
            _publishSlots.Release();
            throw;
        }
    }

    private async ValueTask<IChannel?> RentChannelAsync(
        PublisherChannelPool pool,
        object? publisher,
        CancellationToken cancellationToken)
    {
        PublisherChannelSlot? slot;
        PublisherChannel? owner = null;
        Task? available = null;
        lock (_channelSync)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (!ReferenceEquals(pool, _channelPool))
            {
                return null;
            }

            slot = pool.TryRentSlot(publisher);
            if (slot is null)
            {
                available = pool.WaitForSlot();
            }
            else
            {
                if (slot.Current is { } previous
                    && (previous.Channel.Task.IsFaulted
                        || (previous.Channel.Task.IsCompletedSuccessfully && !previous.Channel.Task.Result.IsOpen)))
                {
                    RetireChannel(slot);
                }

                if (slot.Current is null)
                {
                    slot.Current = new PublisherChannel(slot);
                    slot.Current.Creation = CreateChannelAsync(slot.Current, slot.Cleanup, _channelCts.Token);
                }

                owner = slot.Current;
            }
        }

        if (slot is null)
        {
            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _channelCts.Token, pool.RetirementToken);
            try
            {
                await available!.WaitAsync(waitCts.Token);
            }
            catch (OperationCanceledException)
                when (pool.RetirementToken.IsCancellationRequested
                    && !cancellationToken.IsCancellationRequested
                    && !IsDisposed)
            {
            }

            return null;
        }

        var isRented = false;
        try
        {
            // Caller cancellation releases the lease; channel creation remains owned by the dispatcher.
            var channel = await owner!.Channel.Task.WaitAsync(cancellationToken);
            lock (_channelSync)
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);
                if (!ReferenceEquals(slot.Current, owner)
                    || !ReferenceEquals(pool.Connection, CurrentConnection)
                    || !channel.IsOpen)
                {
                    return null;
                }

                _rentedChannels.TryAdd(channel, owner);
                owner.RentCount++;
                isRented = true;
                return channel;
            }
        }
        finally
        {
            if (!isRented)
            {
                lock (_channelSync)
                {
                    pool.ReturnSlot(slot);
                }
            }
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
    /// Returns a publisher lease and retires the channel on which a protocol operation failed.
    /// </summary>
    internal void DiscardChannel(IChannel channel) => ReturnChannel(channel, discard: true);

    private void ReturnChannel(IChannel channel, bool discard)
    {
        lock (_channelSync)
        {
            if (!_rentedChannels.TryGetValue(channel, out var owner))
            {
                return;
            }

            if (--owner.RentCount == 0)
            {
                _rentedChannels.Remove(channel);
            }

            var slot = owner.Slot;
            if (ReferenceEquals(slot.Current, owner)
                && (discard || IsDisposed || !channel.IsOpen
                    || !ReferenceEquals(owner.Connection, CurrentConnection)))
            {
                RetireChannel(slot);
            }

            slot.Pool.ReturnSlot(slot);
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

            if (ex is ChannelAllocationException)
            {
                return;
            }

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
        await ClearChannelsAsync(preserveCurrentConnection: false);
    }

    protected override async Task OnConnectionLostAsync()
    {
        await ClearChannelsAsync(preserveCurrentConnection: true);
        StartReconnecting();
    }

    private async Task ClearChannelsAsync(bool preserveCurrentConnection)
    {
        PublisherChannelPool pool;
        Task[] cleanup;
        lock (_channelSync)
        {
            if (_channelPool is null
                || (preserveCurrentConnection && !IsDisposed && _channelPool.Connection.IsOpen
                    && ReferenceEquals(_channelPool.Connection, CurrentConnection)))
            {
                return;
            }

            pool = _channelPool;
            _channelPool = null;
            cleanup = new Task[pool.Slots.Count];
            for (var i = 0; i < pool.Slots.Count; i++)
            {
                var slot = pool.Slots[i];
                RetireChannel(slot);
                cleanup[i] = slot.Cleanup;
            }
        }

        await pool.RetireAsync();
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
        await ClearChannelsAsync(preserveCurrentConnection: false);
        _channelCts.Dispose();
    }

    private sealed class PublisherChannelPool
    {
        private readonly CancellationTokenSource _retirementCts = new();
        private readonly Dictionary<object, PublisherChannelSlot> _publishers = new(ReferenceEqualityComparer.Instance);
        private readonly LinkedList<PublisherChannelSlot> _available = new();
        private readonly int _capacity;
        private TaskCompletionSource? _slotAvailable;

        public PublisherChannelPool(IConnection connection)
        {
            Connection = connection;
            RetirementToken = _retirementCts.Token;
            _capacity = connection.ChannelMax == 0
                ? MaxPublisherChannels
                : Math.Min(MaxPublisherChannels, (int)connection.ChannelMax);
            Slots = new List<PublisherChannelSlot>(_capacity);
        }

        public IConnection Connection { get; }

        public List<PublisherChannelSlot> Slots { get; }

        public CancellationToken RetirementToken { get; }

        public PublisherChannelSlot? TryRentSlot(object? publisher)
        {
            PublisherChannelSlot slot;
            if (publisher is not null && _publishers.TryGetValue(publisher, out var shared))
            {
                slot = shared;
                if (slot.ActiveRentals == 0)
                {
                    _available.Remove(slot.Node);
                }
            }
            else
            {
                if (_available.Last is { } available)
                {
                    slot = available.Value;
                    _available.RemoveLast();
                    if (slot.Publisher is { } previousPublisher)
                    {
                        _publishers.Remove(previousPublisher);
                    }
                }
                else if (Slots.Count < _capacity)
                {
                    slot = new PublisherChannelSlot(this);
                    Slots.Add(slot);
                }
                else
                {
                    return null;
                }

                slot.Publisher = publisher;
                if (publisher is not null)
                {
                    _publishers.Add(publisher, slot);
                    SignalSlotAvailable();
                }
            }

            slot.ActiveRentals++;
            return slot;
        }

        public void ReturnSlot(PublisherChannelSlot slot)
        {
            if (--slot.ActiveRentals == 0)
            {
                _available.AddLast(slot.Node);
                SignalSlotAvailable();
            }
        }

        public Task WaitForSlot()
            => (_slotAvailable ??= new(TaskCreationOptions.RunContinuationsAsynchronously)).Task;

        private void SignalSlotAvailable()
        {
            var available = _slotAvailable;
            _slotAvailable = null;
            available?.TrySetResult();
        }

        public async Task RetireAsync()
        {
            await _retirementCts.CancelAsync();
            _retirementCts.Dispose();
        }
    }

    private sealed class PublisherChannelSlot
    {
        public PublisherChannelSlot(PublisherChannelPool pool)
        {
            Pool = pool;
            Node = new LinkedListNode<PublisherChannelSlot>(this);
        }

        public PublisherChannelPool Pool { get; }

        public LinkedListNode<PublisherChannelSlot> Node { get; }

        public object? Publisher { get; set; }

        public int ActiveRentals { get; set; }

        public PublisherChannel? Current { get; set; }

        public Task Cleanup { get; set; } = Task.CompletedTask;
    }

    private sealed class PublisherChannel(PublisherChannelSlot slot)
    {
        public PublisherChannelSlot Slot { get; } = slot;

        public IConnection Connection => Slot.Pool.Connection;

        public int RentCount { get; set; }

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
