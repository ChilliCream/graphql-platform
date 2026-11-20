using System.Threading.Channels;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Owns the RabbitMQ connection that publishes messages, and leases its channels to one publish at a time.
/// </summary>
public sealed class RabbitMQDispatcher : IAsyncDisposable
{
    private static readonly CreateChannelOptions s_publishChannelOptions = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true,
        outstandingPublisherConfirmationsRateLimiter: null);

    private readonly RabbitMQDispatcherOptions _options;
    private readonly RabbitMQConnectionManager _connectionManager;

    /// <summary>
    /// The slots that no publish holds. A slot is an idle channel, or <c>null</c> when its holder opens a
    /// channel for it.
    /// </summary>
    private readonly Channel<ChannelLease?> _slots = Channel.CreateUnbounded<ChannelLease?>();

    private int _createdSlots;
    private volatile bool _isDisposed;

    /// <summary>
    /// Creates a dispatcher.
    /// </summary>
    /// <param name="options">The limits of the dispatcher.</param>
    /// <param name="connectionManager">The manager of the connection that publishes messages.</param>
    internal RabbitMQDispatcher(RabbitMQDispatcherOptions options, RabbitMQConnectionManager connectionManager)
    {
        _options = options;
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// Gets whether the broker blocks the connection, which it does during a memory or disk alarm.
    /// </summary>
    public bool IsBlocked => _connectionManager.IsBlocked;

    /// <summary>
    /// Gets whether a ready connection exists.
    /// </summary>
    public bool IsConnected => _connectionManager.IsConnected;

    /// <summary>
    /// Gets the number of slots, which is not larger than the channel limit of the ready connection. A
    /// channel limit of zero means that the broker sets no limit.
    /// </summary>
    private int MaxSlots
        => _connectionManager.Connection is { ChannelMax: > 0 and var channelMax }
            ? Math.Min(_options.MaxPublisherChannels, channelMax)
            : _options.MaxPublisherChannels;

    /// <summary>
    /// Connects to the broker.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken) => _connectionManager.StartAsync(cancellationToken);

    /// <summary>
    /// Leases a channel to one publish, waiting while all channels are leased, while the broker blocks the
    /// connection and while there is no connection. The caller disposes the lease to give the channel back.
    /// </summary>
    internal async ValueTask<ChannelLease> RentChannelAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        // An idle channel is taken before a new slot is made, so the dispatcher opens no more channels than
        // the publishes need at the same time.
        if (!_slots.Reader.TryRead(out var idle) && Interlocked.Increment(ref _createdSlots) > MaxSlots)
        {
            Interlocked.Decrement(ref _createdSlots);
            try
            {
                // A publish holds each slot. Wait until one is given back.
                idle = await _slots.Reader.ReadAsync(cancellationToken);
            }
            catch (ChannelClosedException)
            {
                throw ThrowHelper.DispatcherDisposed();
            }
        }

        try
        {
            while (true)
            {
                // The broker does not read from a blocked connection. A message written to it is accepted
                // when the broker reads again, also when its publish has given up by then.
                await _connectionManager.WaitWhileBlockedAsync(cancellationToken);

                if (idle is { Channel.IsOpen: true } lease)
                {
                    lease.IsReusable = false;
                    return lease;
                }

                if (idle is { } closed)
                {
                    // The channel closed while it was idle, for example with its connection.
                    _ = _connectionManager.CloseChannelAsync(closed.Channel);
                    idle = null;
                }

                try
                {
                    var connection = await _connectionManager.GetConnectionAsync(cancellationToken);
                    var channel = await _connectionManager.OpenChannelAsync(
                        connection,
                        s_publishChannelOptions,
                        _options.ChannelOpenTimeout,
                        cancellationToken);

                    return new ChannelLease(this, connection, channel);
                }
                catch (ChannelOpenBlockedException)
                {
                    // The broker blocked the connection after the check above. Wait until it reads again.
                }
            }
        }
        catch
        {
            _slots.Writer.TryWrite(idle);
            throw;
        }
    }

    /// <summary>
    /// Takes a lease back. The channel is leased again only when the lease is reusable. Otherwise the
    /// channel is closed.
    /// </summary>
    internal void Return(ChannelLease lease)
    {
        if (lease.IsReusable && lease.Channel.IsOpen && _slots.Writer.TryWrite(lease))
        {
            return;
        }

        _ = _connectionManager.CloseChannelAsync(lease.Channel);
        _slots.Writer.TryWrite(null);
    }

    /// <summary>
    /// Ends every wait for a channel, closes the idle channels and closes the connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;
        _slots.Writer.TryComplete();

        while (_slots.Reader.TryRead(out var idle))
        {
            if (idle is { } lease)
            {
                _ = _connectionManager.CloseChannelAsync(lease.Channel);
            }
        }

        await _connectionManager.DisposeAsync();
    }
}
