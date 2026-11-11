using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Owns the RabbitMQ connection that publishes messages, and leases its channels to one publish at a time.
/// </summary>
internal sealed partial class RabbitMQDispatcher : IAsyncDisposable
{
    /// <summary>
    /// The largest number of publisher channels per connection, which is also the largest number of
    /// publishes in flight.
    /// </summary>
    internal const int MaxPublisherChannels = 100;

    private static readonly CreateChannelOptions s_publishChannelOptions = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true,
        outstandingPublisherConfirmationsRateLimiter: null);

    private readonly ILogger _logger;
    private readonly RabbitMQConnectionManager _connection;
    private readonly object _sync = new();
    private PublisherChannelPool? _pool;
    private bool _isDisposed;

    /// <summary>
    /// Creates a dispatcher.
    /// </summary>
    /// <param name="logger">The logger for publish events.</param>
    /// <param name="connection">The manager of the connection that publishes messages.</param>
    public RabbitMQDispatcher(ILogger<RabbitMQDispatcher> logger, RabbitMQConnectionManager connection)
    {
        _logger = logger;
        _connection = connection;
    }

    /// <summary>
    /// Gets the manager of the connection that publishes messages.
    /// </summary>
    public RabbitMQConnectionManager Connection => _connection;

    /// <summary>
    /// Gets whether a ready connection exists.
    /// </summary>
    public bool IsConnected => _connection.IsConnected;

    /// <summary>
    /// Gets whether the dispatcher was disposed, after which no publish can succeed.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _isDisposed);

    /// <summary>
    /// Gets or sets how long opening a publisher channel may take before its connection is closed and replaced.
    /// </summary>
    public TimeSpan ChannelOpenTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets how long a publish waits for the broker's confirmation, including the wait for a
    /// connection and all retries. <see cref="Timeout.InfiniteTimeSpan"/> waits until the caller cancels.
    /// </summary>
    public TimeSpan PublishTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Connects and starts the stalled publish check.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _connection.StartAsync(cancellationToken);
        StartStalledPublishCheck();
    }

    /// <summary>
    /// Leases a channel of the ready connection to one publish, waiting while there is no connection or all
    /// channels are leased. The caller gives the lease back with <see cref="Return"/>.
    /// </summary>
    public async ValueTask<PublisherLease> RentChannelAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var connection = await _connection.GetConnectionAsync(cancellationToken);
            if (GetPool(connection) is not { } pool)
            {
                // The connection was replaced in the meantime.
                continue;
            }

            await pool.AcquireAsync(cancellationToken);

            if (pool.IsRetired)
            {
                // The connection was replaced while this publish waited. The other waiters of the pool follow.
                pool.Release(channel: null, reusable: false);
                continue;
            }

            var ownsPermit = true;
            try
            {
                while (true)
                {
                    while (pool.TryTakeIdle(out var idle))
                    {
                        if (idle.IsOpen)
                        {
                            return new PublisherLease(pool, idle);
                        }

                        _ = RabbitMQConnectionManager.CloseChannelAsync(idle);
                    }

                    if (!_connection.IsBlocked)
                    {
                        break;
                    }

                    // The broker does not read from a blocked connection, so it would not answer a request to
                    // open a channel. The publish waits here and takes a channel that became idle meanwhile.
                    await _connection.WaitWhileBlockedAsync(cancellationToken);
                    if (pool.IsRetired || !ReferenceEquals(_connection.Connection, connection))
                    {
                        break;
                    }
                }

                if (pool.IsRetired || !ReferenceEquals(_connection.Connection, connection))
                {
                    // The connection was lost while this publish waited.
                    ownsPermit = false;
                    pool.Release(channel: null, reusable: false);
                    continue;
                }

                var opening = _connection.OpenChannelAsync(connection, s_publishChannelOptions, ChannelOpenTimeout);
                try
                {
                    var channel = await opening.WaitAsync(cancellationToken);
                    LogPublisherChannelOpened(channel);
                    return new PublisherLease(pool, channel);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // The caller gave up. The channel keeps its permit until it is open and goes to the pool.
                    ownsPermit = false;
                    _ = ReturnWhenOpenedAsync(pool, opening);
                    throw;
                }
                catch (ChannelOpenBlockedException)
                {
                    ownsPermit = false;
                    OnChannelNumberLost(pool);
                    throw;
                }
            }
            catch when (ownsPermit)
            {
                pool.Release(channel: null, reusable: false);
                throw;
            }
        }
    }

    /// <summary>
    /// Gives a lease back. The channel is leased again only when <paramref name="reusable"/> is <c>true</c>,
    /// which requires that the broker answered the last publish on it. Otherwise the channel is closed.
    /// </summary>
    public void Return(PublisherLease lease, bool reusable) => lease.Pool.Release(lease.Channel, reusable);

    private async Task ReturnWhenOpenedAsync(PublisherChannelPool pool, Task<IChannel> opening)
    {
        IChannel? channel = null;
        try
        {
            channel = await opening;
        }
        catch (ChannelOpenBlockedException)
        {
            OnChannelNumberLost(pool);
            return;
        }
        catch
        {
            // The channel could not be opened.
        }

        pool.Release(channel, reusable: channel is not null);
    }

    /// <summary>
    /// Keeps the permit of a channel number the client lost to a request the blocked connection did not
    /// answer, so the connection never has more channels than permits. A connection that lost half of its
    /// channels this way is replaced.
    /// </summary>
    private void OnChannelNumberLost(PublisherChannelPool pool)
    {
        if (pool.LoseChannelNumber())
        {
            _connection.ReportUnresponsive(
                pool.Connection,
                "half of its publisher channels were lost to requests the broker did not answer while it blocked the connection");
        }
    }

    private PublisherChannelPool? GetPool(IConnection connection)
    {
        var pool = Volatile.Read(ref _pool);
        if (pool is not null && ReferenceEquals(pool.Connection, connection))
        {
            return pool;
        }

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            pool = _pool;
            if (pool is not null && ReferenceEquals(pool.Connection, connection))
            {
                return pool;
            }

            // Only the ready connection gets a pool, so a caller that still holds a lost connection asks again.
            if (!ReferenceEquals(_connection.Connection, connection))
            {
                return null;
            }

            // The pool of a lost connection retired itself when the connection shut down.
            pool?.Retire();
            pool = new PublisherChannelPool(connection, MaxPublisherChannels);
            Volatile.Write(ref _pool, pool);
            return pool;
        }
    }

    /// <summary>
    /// Stops the stalled publish check, closes the publisher channels and closes the connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        PublisherChannelPool? pool;
        lock (_sync)
        {
            _isDisposed = true;
            pool = _pool;
            _pool = null;
        }

        StopStalledPublishCheck();
        pool?.Retire();
        await _connection.DisposeAsync();
    }
}

/// <summary>
/// A publisher channel that is leased to one publish.
/// </summary>
/// <param name="Pool">The pool the channel belongs to.</param>
/// <param name="Channel">The leased channel, which has publisher confirms enabled.</param>
internal readonly record struct PublisherLease(PublisherChannelPool Pool, IChannel Channel)
{
    /// <summary>
    /// Gets the connection the channel belongs to.
    /// </summary>
    public IConnection Connection => Pool.Connection;
}

/// <summary>
/// The publisher channels of one connection. A permit bounds the channels and the publishes in flight, and
/// channels the broker answered on last wait on a stack for the next publish.
/// </summary>
internal sealed class PublisherChannelPool
{
    // Larger than any number of waiters, and far from an overflow of the permit count.
    private const int AllWaiters = 1 << 30;

    private readonly SemaphoreSlim _permits;
    private readonly Stack<IChannel> _idle;
    private readonly int _capacity;
    private int _lostChannelNumbers;
    private bool _isRetired;

    public PublisherChannelPool(IConnection connection, int maxChannels)
    {
        Connection = connection;

        // A channel maximum of zero means that the broker sets no limit.
        _capacity = connection.ChannelMax == 0 ? maxChannels : Math.Min(maxChannels, connection.ChannelMax);
        _permits = new SemaphoreSlim(_capacity);
        _idle = new Stack<IChannel>(_capacity);

        connection.ConnectionShutdownAsync += (_, _) =>
        {
            Retire();
            return Task.CompletedTask;
        };

        // A connection that closed before the subscription raised its shutdown event to nobody.
        if (!connection.IsOpen)
        {
            Retire();
        }
    }

    /// <summary>
    /// Gets the connection the channels belong to.
    /// </summary>
    public IConnection Connection { get; }

    /// <summary>
    /// Gets whether the connection was lost or the dispatcher was disposed, after which the pool leases
    /// no channel.
    /// </summary>
    public bool IsRetired => Volatile.Read(ref _isRetired);

    /// <summary>
    /// Takes a permit, waiting while all are taken. The wait also ends when the pool is retired.
    /// </summary>
    public ValueTask AcquireAsync(CancellationToken cancellationToken)
        => _permits.Wait(0, CancellationToken.None)
            ? ValueTask.CompletedTask
            : new ValueTask(_permits.WaitAsync(cancellationToken));

    /// <summary>
    /// Takes the channel that was returned last, if there is one.
    /// </summary>
    public bool TryTakeIdle(out IChannel channel)
    {
        lock (_idle)
        {
            return _idle.TryPop(out channel!);
        }
    }

    /// <summary>
    /// Gives a permit back, and keeps its channel for the next publish when it is reusable and still open.
    /// A channel that is not kept is closed first, so the connection never has more channels than permits.
    /// </summary>
    public void Release(IChannel? channel, bool reusable)
    {
        if (channel is not null)
        {
            var isKept = false;
            if (reusable && channel.IsOpen)
            {
                lock (_idle)
                {
                    if (!_isRetired)
                    {
                        _idle.Push(channel);
                        isKept = true;
                    }
                }
            }

            if (!isKept)
            {
                _ = CloseAndReleaseAsync(channel);
                return;
            }
        }

        _permits.Release();
    }

    private async Task CloseAndReleaseAsync(IChannel channel)
    {
        // The permit follows the channel number, which the client gives back when the close has completed.
        await RabbitMQConnectionManager.CloseChannelCompletelyAsync(channel);
        _permits.Release();
    }

    /// <summary>
    /// Keeps a permit for good, because the client lost its channel number. Returns <c>true</c> when half of
    /// the permits are gone.
    /// </summary>
    public bool LoseChannelNumber() => Interlocked.Increment(ref _lostChannelNumbers) * 2 >= _capacity;

    /// <summary>
    /// Closes the idle channels and ends every wait for a permit. Channels that are returned later are
    /// closed as well.
    /// </summary>
    public void Retire()
    {
        IChannel[] idle;
        lock (_idle)
        {
            if (_isRetired)
            {
                return;
            }

            _isRetired = true;
            idle = _idle.ToArray();
            _idle.Clear();
        }

        // Whoever takes a permit of a retired pool gives it back and asks for the pool of the next connection.
        _permits.Release(AllWaiters);

        foreach (var channel in idle)
        {
            _ = RabbitMQConnectionManager.CloseChannelAsync(channel);
        }
    }
}
