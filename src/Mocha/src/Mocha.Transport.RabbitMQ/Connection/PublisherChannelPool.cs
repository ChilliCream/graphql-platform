using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

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
