using System.Diagnostics.Tracing;

namespace eShop.Gateway;

/// <summary>
/// Listens to PathSegmentPool ETW events and logs aggregated usage metrics.
/// </summary>
internal sealed class PathSegmentPoolDiagnostics : EventListener, IHostedService
{
    private readonly Timer _timer;

    private int _poolId;
    private int _segmentSize;
    private int _maxArrays;
    private long _maxBytes;

    private long _rented;
    private long _returned;
    private long _exhausted;
    private long _dropped;
    private long _allocated;
    private long _trimmedEvents;
    private int _lastTrimRemaining;
    private int _lastTrimInUse;
    private int _peakInUse;

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
                    if (e.Payload[0] is int poolId)
                    {
                        _poolId = poolId;
                    }
                    if (e.Payload[1] is int segmentSize)
                    {
                        _segmentSize = segmentSize;
                    }
                    if (e.Payload[2] is int maxArrays)
                    {
                        _maxArrays = maxArrays;
                    }
                    if (e.Payload[3] is long maxBytes)
                    {
                        _maxBytes = maxBytes;
                    }
                }
                break;

            case 2:
                Interlocked.Increment(ref _rented);
                if (e.Payload is { Count: >= 4 } && e.Payload[3] is int inUseRent)
                {
                    UpdatePeakInUse(inUseRent);
                }
                break;

            case 3:
                Interlocked.Increment(ref _returned);
                break;

            case 4:
                Interlocked.Increment(ref _exhausted);
                break;

            case 5:
                Interlocked.Increment(ref _dropped);
                break;

            case 6:
                Interlocked.Increment(ref _allocated);
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

    private void UpdatePeakInUse(int inUse)
    {
        int current;
        do
        {
            current = _peakInUse;
            if (inUse <= current)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref _peakInUse, inUse, current) != current);
    }

    private void LogSnapshot(object? state)
    {
        var rented = Interlocked.Read(ref _rented);
        var returned = Interlocked.Read(ref _returned);
        var exhausted = Interlocked.Read(ref _exhausted);
        var dropped = Interlocked.Read(ref _dropped);
        var allocated = Interlocked.Read(ref _allocated);
        var trimmedEvents = Interlocked.Read(ref _trimmedEvents);
        var outstanding = rented - returned;

        Console.WriteLine(
            "[PathSegmentPool] PoolId={0}, SegmentSize={1}, MaxArrays={2}, MaxBytes={3}, "
            + "Rented={4}, Returned={5}, Outstanding={6}, PeakInUse={7}, "
            + "Exhausted={8}, Allocated={9}, Dropped={10}, "
            + "TrimmedEvents={11}, LastTrimRemaining={12}, LastTrimInUse={13}",
            _poolId,
            _segmentSize,
            _maxArrays,
            _maxBytes,
            rented,
            returned,
            outstanding,
            _peakInUse,
            exhausted,
            allocated,
            dropped,
            trimmedEvents,
            _lastTrimRemaining,
            _lastTrimInUse);
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
}
