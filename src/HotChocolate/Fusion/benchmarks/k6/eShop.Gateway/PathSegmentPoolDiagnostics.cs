using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Text;

namespace eShop.Gateway;

/// <summary>
/// Listens to PathSegmentPool ETW events and logs aggregated usage metrics across all stripes.
/// </summary>
internal sealed class PathSegmentPoolDiagnostics : EventListener, IHostedService
{
    private readonly Timer _timer;
    private readonly ConcurrentDictionary<int, StripeCounters> _stripes = new();

    private int _poolCount;
    private int _segmentSize;
    private long _maxArraysTotal;
    private long _maxBytesTotal;

    private long _rented;
    private long _returned;
    private long _exhausted;
    private long _dropped;
    private long _allocated;
    private long _trimmedEvents;
    private int _lastTrimRemaining;
    private int _lastTrimInUse;
    private int _peakInUsePerStripe;

    public PathSegmentPoolDiagnostics()
    {
        _timer = new Timer(LogSnapshot, null, Timeout.Infinite, Timeout.Infinite);
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "HotChocolate-Fusion-PathSegmentPool")
        {
            EnableEvents(eventSource, EventLevel.Verbose);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        // Event IDs from PathSegmentPoolEventSource:
        // 1 = PoolCreated     (PoolId, SegmentSize, Arrays, TotalBytes)
        // 2 = SegmentRented   (ArrayId, Length, PoolId, InUse)
        // 3 = SegmentReturned (ArrayId, Length, PoolId, InUse)
        // 4 = PoolExhausted   (PoolId, MaxArrays)
        // 5 = SegmentDropped  (ArrayId, Length, PoolId)
        // 6 = SegmentAllocated(ArrayId, Length, PoolId)
        // 7 = PoolTrimmed     (PoolId, Trimmed, Remaining, InUse)
        switch (e.EventId)
        {
            case 1:
                if (e.Payload is { Count: >= 4 })
                {
                    Interlocked.Increment(ref _poolCount);

                    if (e.Payload[1] is int segmentSize)
                    {
                        _segmentSize = segmentSize;
                    }
                    if (e.Payload[2] is int maxArrays)
                    {
                        Interlocked.Add(ref _maxArraysTotal, maxArrays);
                    }
                    if (e.Payload[3] is long maxBytes)
                    {
                        Interlocked.Add(ref _maxBytesTotal, maxBytes);
                    }
                }
                break;

            case 2:
                Interlocked.Increment(ref _rented);
                if (e.Payload is { Count: >= 4 } && e.Payload[3] is int inUseRent)
                {
                    UpdatePeakInUsePerStripe(inUseRent);
                }
                break;

            case 3:
                Interlocked.Increment(ref _returned);
                break;

            case 4:
                Interlocked.Increment(ref _exhausted);
                if (e.Payload is { Count: >= 1 } && e.Payload[0] is int exhaustedPoolId)
                {
                    Interlocked.Increment(ref GetStripe(exhaustedPoolId).Exhausted);
                }
                break;

            case 5:
                Interlocked.Increment(ref _dropped);
                if (e.Payload is { Count: >= 3 } && e.Payload[2] is int droppedPoolId)
                {
                    Interlocked.Increment(ref GetStripe(droppedPoolId).Dropped);
                }
                break;

            case 6:
                Interlocked.Increment(ref _allocated);
                if (e.Payload is { Count: >= 3 } && e.Payload[2] is int allocatedPoolId)
                {
                    Interlocked.Increment(ref GetStripe(allocatedPoolId).Allocated);
                }
                break;

            case 7:
                Interlocked.Increment(ref _trimmedEvents);
                if (e.Payload is { Count: >= 4 })
                {
                    if (e.Payload[2] is int remaining)
                    {
                        _lastTrimRemaining = remaining;
                    }
                    if (e.Payload[3] is int inUseTrim)
                    {
                        _lastTrimInUse = inUseTrim;
                    }
                }
                break;
        }
    }

    private StripeCounters GetStripe(int poolId)
        => _stripes.GetOrAdd(poolId, static _ => new StripeCounters());

    private void UpdatePeakInUsePerStripe(int inUse)
    {
        int current;
        do
        {
            current = _peakInUsePerStripe;
            if (inUse <= current)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref _peakInUsePerStripe, inUse, current) != current);
    }

    private void LogSnapshot(object? state)
    {
        var rented = Interlocked.Read(ref _rented);
        var returned = Interlocked.Read(ref _returned);
        var exhausted = Interlocked.Read(ref _exhausted);
        var dropped = Interlocked.Read(ref _dropped);
        var allocated = Interlocked.Read(ref _allocated);
        var trimmedEvents = Interlocked.Read(ref _trimmedEvents);
        var maxArraysTotal = Interlocked.Read(ref _maxArraysTotal);
        var maxBytesTotal = Interlocked.Read(ref _maxBytesTotal);
        var outstanding = rented - returned;

        Console.WriteLine(
            "[PathSegmentPool] Pools={0}, SegmentSize={1}, MaxArraysTotal={2}, MaxBytesTotal={3}, "
            + "Rented={4}, Returned={5}, Outstanding={6}, PeakInUsePerStripe={7}, "
            + "Exhausted={8}, Allocated={9}, Dropped={10}, "
            + "TrimmedEvents={11}, LastTrimRemaining(any stripe)={12}, LastTrimInUse(any stripe)={13}",
            Volatile.Read(ref _poolCount),
            _segmentSize,
            maxArraysTotal,
            maxBytesTotal,
            rented,
            returned,
            outstanding,
            _peakInUsePerStripe,
            exhausted,
            allocated,
            dropped,
            trimmedEvents,
            _lastTrimRemaining,
            _lastTrimInUse);

        // Per-stripe Dropped and Allocated near zero at steady state is the acceptance criterion:
        // it proves rents and returns are matched on each pool so pooling is not being defeated.
        var spread = new StringBuilder("[PathSegmentPool] Per-stripe spread:");
        var first = true;
        foreach (var poolId in _stripes.Keys.OrderBy(static id => id))
        {
            var stripe = _stripes[poolId];
            spread.Append(first ? " " : ", ");
            spread.Append(
                $"#{poolId}(Dropped={Interlocked.Read(ref stripe.Dropped)}, "
                + $"Allocated={Interlocked.Read(ref stripe.Allocated)}, "
                + $"Exhausted={Interlocked.Read(ref stripe.Exhausted)})");
            first = false;
        }

        if (first)
        {
            spread.Append(" none");
        }

        Console.WriteLine(spread.ToString());
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[PathSegmentPool] Diagnostics started");
        _timer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        LogSnapshot(null);
        Console.WriteLine("[PathSegmentPool] Diagnostics stopped - final snapshot logged above");
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }

    private sealed class StripeCounters
    {
        public long Dropped;
        public long Allocated;
        public long Exhausted;
    }
}
