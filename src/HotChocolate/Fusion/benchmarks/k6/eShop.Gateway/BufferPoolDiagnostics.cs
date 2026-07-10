using System.Diagnostics.Tracing;

namespace eShop.Gateway;

/// <summary>
/// Listens to FixedSizeArrayPool ETW events and tracks rent/return balance
/// to detect leaks or pool exhaustion.
/// </summary>
internal sealed class BufferPoolDiagnostics : EventListener, IHostedService
{
    private readonly ILogger<BufferPoolDiagnostics> _logger;
    private readonly Timer _timer;

    private long _rented;
    private long _returned;
    private long _poolExhausted;
    private long _bufferDropped;
    private long _bufferAllocated;
    private long _poolTrimmed;
    private int _lastTrimRemaining;
    private int _lastTrimInUse;
    private int _peakInUse;

    public BufferPoolDiagnostics(ILogger<BufferPoolDiagnostics> logger)
    {
        _logger = logger;
        _timer = new Timer(LogSnapshot, null, Timeout.Infinite, Timeout.Infinite);
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "HotChocolate-Buffers-FixedSizeArrayPool")
        {
            EnableEvents(eventSource, EventLevel.Verbose);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        // Event IDs from FixedSizeArrayPoolEventSource:
        // 1 = PoolCreated     (PoolId, Chunks, TotalBytes)
        // 2 = BufferRented    (BufferId, Size, PoolId, InUse)
        // 3 = BufferReturned  (BufferId, Size, PoolId, InUse)
        // 4 = PoolExhausted   (PoolId, MaxChunks)
        // 5 = BufferDropped   (BufferId, Size, PoolId)
        // 6 = BufferAllocated (BufferId, Size, PoolId)
        // 7 = PoolTrimmed     (PoolId, Trimmed, Remaining, InUse)

        switch (e.EventId)
        {
            case 2: // BufferRented
                Interlocked.Increment(ref _rented);
                if (e.Payload is { Count: >= 4 } && e.Payload[3] is int inUseRent)
                {
                    UpdatePeakInUse(inUseRent);
                }
                break;

            case 3: // BufferReturned
                Interlocked.Increment(ref _returned);
                break;

            case 4: // PoolExhausted
                Interlocked.Increment(ref _poolExhausted);
                break;

            case 5: // BufferDropped
                Interlocked.Increment(ref _bufferDropped);
                break;

            case 6: // BufferAllocated
                Interlocked.Increment(ref _bufferAllocated);
                break;

            case 7: // PoolTrimmed
                Interlocked.Increment(ref _poolTrimmed);
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
        var exhausted = Interlocked.Read(ref _poolExhausted);
        var dropped = Interlocked.Read(ref _bufferDropped);
        var allocated = Interlocked.Read(ref _bufferAllocated);
        var trimmed = Interlocked.Read(ref _poolTrimmed);
        var trimRemaining = _lastTrimRemaining;
        var trimInUse = _lastTrimInUse;
        var peak = _peakInUse;
        var outstanding = rented - returned;

        _logger.LogInformation(
            "[BufferPool] Rented={Rented}, Returned={Returned}, Outstanding={Outstanding}, "
            + "PeakInUse={PeakInUse}, PoolExhausted={PoolExhausted}, "
            + "Dropped={Dropped}, Allocated={Allocated}, "
            + "Trimmed={Trimmed}, TrimRemaining={TrimRemaining}, TrimInUse={TrimInUse}",
            rented, returned, outstanding, peak, exhausted, dropped, allocated,
            trimmed, trimRemaining, trimInUse);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[BufferPool] Diagnostics started — pool capacity=128, chunk size=128KB");

        // Log every 5 seconds during the benchmark run.
        _timer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        LogSnapshot(null);
        _logger.LogInformation("[BufferPool] Diagnostics stopped — final snapshot logged above");
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}
