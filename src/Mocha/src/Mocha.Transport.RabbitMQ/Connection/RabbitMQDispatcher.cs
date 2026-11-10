using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Manages the RabbitMQ connection and bounded channel pool used to publish messages.
/// </summary>
public sealed partial class RabbitMQDispatcher(
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
            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, DisposalToken);
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

                channel = await TryRentChannelAsync(pool, publisher, cancellationToken);
            }

            return channel;
        }
        catch
        {
            _publishSlots.Release();
            throw;
        }
    }

    /// <summary>
    /// Rents a channel from the pool, or returns <c>null</c> when the caller has to try again because the
    /// pool was replaced, it had to wait for a free slot, or the channel is no longer usable.
    /// </summary>
    private async ValueTask<IChannel?> TryRentChannelAsync(
        PublisherChannelPool pool,
        object? publisher,
        CancellationToken cancellationToken)
    {
        PublisherChannelSlot? slot;
        PublisherChannel? publisherChannel = null;
        Task? slotAvailable = null;
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
                slotAvailable = pool.WaitForSlot();
            }
            else
            {
                if (slot.Current is { IsUnusable: true })
                {
                    RetireChannel(slot);
                }

                if (slot.Current is null)
                {
                    slot.Current = new PublisherChannel(slot);
                    slot.Current.Creation = CreateChannelAsync(slot.Current, slot.Cleanup, DisposalToken);
                }

                publisherChannel = slot.Current;
            }
        }

        if (slot is null)
        {
            await WaitForSlotAsync(pool, slotAvailable!, cancellationToken);
            return null;
        }

        var isRented = false;
        try
        {
            // Caller cancellation releases the lease; channel creation remains owned by the dispatcher.
            var channel = await publisherChannel!.Opened.WaitAsync(cancellationToken);
            lock (_channelSync)
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);
                if (!ReferenceEquals(slot.Current, publisherChannel)
                    || !ReferenceEquals(pool.Connection, CurrentConnection)
                    || !channel.IsOpen)
                {
                    return null;
                }

                _rentedChannels.TryAdd(channel, publisherChannel);
                publisherChannel.RentCount++;
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

    private async Task WaitForSlotAsync(
        PublisherChannelPool pool,
        Task slotAvailable,
        CancellationToken cancellationToken)
    {
        using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            DisposalToken,
            pool.RetirementToken);
        try
        {
            await slotAvailable.WaitAsync(waitCts.Token);
        }
        catch (OperationCanceledException)
            when (pool.RetirementToken.IsCancellationRequested
                && !cancellationToken.IsCancellationRequested
                && !IsDisposed)
        {
            // The pool was replaced, and the caller rents from its successor.
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
            if (!_rentedChannels.TryGetValue(channel, out var publisherChannel))
            {
                return;
            }

            if (--publisherChannel.RentCount == 0)
            {
                _rentedChannels.Remove(channel);
            }

            var slot = publisherChannel.Slot;
            if (ReferenceEquals(slot.Current, publisherChannel)
                && (discard || IsDisposed || !channel.IsOpen
                    || !ReferenceEquals(publisherChannel.Connection, CurrentConnection)))
            {
                RetireChannel(slot);
            }

            slot.Pool.ReturnSlot(slot);
        }

        _publishSlots.Release();
    }

    private static void RetireChannel(PublisherChannelSlot slot)
    {
        if (slot.Current is { } publisherChannel)
        {
            slot.Current = null;
            slot.Cleanup = DisposePublisherChannelAsync(publisherChannel);
        }
    }

    private async Task CreateChannelAsync(
        PublisherChannel publisherChannel,
        Task cleanup,
        CancellationToken cancellationToken)
    {
        await Task.Yield();
        var connection = publisherChannel.Connection;
        using var _ = BeginLogScope(connection);
        using var creationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        creationCts.CancelAfter(ChannelCreationTimeout);
        Task<IChannel>? opening = null;
        try
        {
            await cleanup.WaitAsync(creationCts.Token);
            opening = connection.CreateChannelAsync(s_publishChannelOptions, creationCts.Token);
            var channel = await opening.WaitAsync(creationCts.Token);
            TrackChannel(connection, channel);
            Logger.CreatedNewChannel(ConnectionName, channel.ChannelNumber);
            publisherChannel.SetOpened(channel);
        }
        catch (Exception ex)
        {
            publisherChannel.SetFailed(ex);

            if (ex is ChannelAllocationException)
            {
                Logger.PublisherChannelLimitReached(ex, ConnectionName);
                return;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                Logger.PublisherChannelCreationFailed(ex, ConnectionName);
            }

            // Close the connection when opening a publisher channel fails.
            try
            {
                await connection.AbortAsync();
            }
            catch
            {
                // The connection is already closing.
            }

            if (opening is not null)
            {
                try
                {
                    await CloseChannelAsync(await opening, CancellationToken.None);
                }
                catch
                {
                    // Opening the channel failed.
                }
            }

            StartReconnecting();
        }
    }

    internal IConnection GetChannelConnection(IChannel channel)
    {
        lock (_channelSync)
        {
            return _rentedChannels[channel].Connection;
        }
    }

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

    private static async Task DisposePublisherChannelAsync(PublisherChannel publisherChannel)
    {
        await publisherChannel.Creation;
        if (publisherChannel.Opened.IsCompletedSuccessfully)
        {
            await CloseChannelAsync(publisherChannel.Opened.Result, CancellationToken.None);
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await ClearChannelsAsync(preserveCurrentConnection: false);
    }
}
