using System.Diagnostics.Tracing;
using System.Globalization;

namespace eShop.Gateway.BenchmarkDiagnostics;

internal sealed class FusionPoolDiagnostics(
    IHostEnvironment environment,
    ILogger<FusionPoolDiagnostics> logger) : EventListener, IHostedService
{
    private const string FixedSizeArrayPoolSource = "HotChocolate-Buffers-FixedSizeArrayPool";
    private const string PathSegmentPoolSource = "HotChocolate-Fusion-PathSegmentPool";
    private const string ResultStorePoolSource = "HotChocolate-Fusion-FetchResultStorePool";
    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromSeconds(5);

    private readonly object _stateLock = new();
    private readonly object _writeLock = new();

    private readonly PoolState _bufferPool = new("BufferPool", FixedSizeArrayPoolSource);
    private readonly PoolState _pathPool = new("PathSegmentPool", PathSegmentPoolSource);
    private readonly PoolState _resultStorePool = new("FetchResultStorePool", ResultStorePoolSource);
    private readonly List<string> _pendingLines = [];

    private StreamWriter? _writer;
    private CancellationTokenSource? _snapshotCts;
    private Task? _snapshotTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var diagnosticsDirectory = ResolveDiagnosticsDirectory();
            Directory.CreateDirectory(diagnosticsDirectory);
            var logPath = Path.Combine(diagnosticsDirectory, "fusion-diagnostics.log");
            _writer = new StreamWriter(
                new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                AutoFlush = true
            };

            FlushPendingLines();
            WriteLine("Diagnostics: started");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FusionPoolDiagnostics] StartAsync failed: {ex.Message}");
        }

        _snapshotCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _snapshotTask = Task.Run(() => SnapshotLoopAsync(_snapshotCts.Token), CancellationToken.None);

        logger.LogInformation("Diagnostics: started");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_snapshotCts is not null)
        {
            await _snapshotCts.CancelAsync().ConfigureAwait(false);
        }

        if (_snapshotTask is not null)
        {
            try
            {
                await _snapshotTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        LogAllPoolSnapshots();
        WriteLine("Diagnostics: stopped");
        logger.LogInformation("Diagnostics: stopped");
        DisposeWriter();
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (!IsTargetSource(eventSource.Name))
        {
            return;
        }

        try
        {
            EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
            lock (_stateLock)
            {
                GetState(eventSource.Name).Attached = true;
            }

            BufferOrWriteLine($"{GetState(eventSource.Name).PoolName}: EventSourceEnabled");
        }
        catch (Exception ex)
        {
            BufferOrWriteLine($"{GetState(eventSource.Name).PoolName}: EventSourceEnableFailed Error={Quote(ex.Message)}");
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (!IsTargetSource(eventData.EventSource.Name))
        {
            return;
        }

        var eventName = eventData.EventName;
        if (string.IsNullOrWhiteSpace(eventName) || eventName == "EventCounters")
        {
            return;
        }

        lock (_stateLock)
        {
            var state = GetState(eventData.EventSource.Name);
            state.Attached = true;
            state.LastEventName = eventName;

            if (eventName.Equals("PoolCreated", StringComparison.OrdinalIgnoreCase))
            {
                if (TryReadInt(eventData, 0, out var poolId))
                {
                    state.PoolId = poolId;
                }

                if (TryReadInt(eventData, 1, out var segmentSize))
                {
                    state.SegmentSize = segmentSize;
                }

                if (TryReadInt(eventData, 2, out var maxArrays))
                {
                    state.MaxArrays = maxArrays;
                }

                if (TryReadLong(eventData, 3, out var maxBytes))
                {
                    state.MaxBytes = maxBytes;
                }

                return;
            }

            if (eventName.Equals("StoreMiss", StringComparison.OrdinalIgnoreCase))
            {
                state.StoreMiss++;
                return;
            }

            if (eventName.Contains("Rented", StringComparison.OrdinalIgnoreCase))
            {
                state.Rented++;
                if (TryReadInt(eventData, 3, out var inUse))
                {
                    state.PeakInUse = Math.Max(state.PeakInUse, inUse);
                }

                return;
            }

            if (eventName.Contains("Returned", StringComparison.OrdinalIgnoreCase))
            {
                state.Returned++;
                return;
            }

            if (eventName.Contains("Exhausted", StringComparison.OrdinalIgnoreCase))
            {
                state.PoolExhausted++;
                return;
            }

            if (eventName.Contains("Dropped", StringComparison.OrdinalIgnoreCase))
            {
                state.Dropped++;
                return;
            }

            if (eventName.Contains("Allocated", StringComparison.OrdinalIgnoreCase))
            {
                state.Allocated++;
                return;
            }

            if (eventName.Contains("Trimmed", StringComparison.OrdinalIgnoreCase))
            {
                state.Trimmed++;
                if (TryReadInt(eventData, 2, out var remaining))
                {
                    state.TrimRemaining = remaining;
                }

                if (TryReadInt(eventData, 3, out var inUse))
                {
                    state.TrimInUse = inUse;
                    state.PeakInUse = Math.Max(state.PeakInUse, inUse);
                }

                return;
            }

            state.OtherEvents++;
        }
    }

    private async Task SnapshotLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            LogAllPoolSnapshots();

            using var timer = new PeriodicTimer(SnapshotInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                LogAllPoolSnapshots();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void LogAllPoolSnapshots()
    {
        PoolSnapshot buffer;
        PoolSnapshot path;
        PoolSnapshot result;

        lock (_stateLock)
        {
            buffer = _bufferPool.Snapshot();
            path = _pathPool.Snapshot();
            result = _resultStorePool.Snapshot();
        }

        LogSnapshot(buffer);
        LogSnapshot(path);
        LogSnapshot(result);
    }

    private void LogSnapshot(PoolSnapshot snapshot)
    {
        var outstanding = snapshot.Rented - snapshot.Returned;
        var maxBytes = snapshot.MaxBytes;
        if (maxBytes == 0 && snapshot.SegmentSize > 0 && snapshot.MaxArrays > 0)
        {
            maxBytes = (long)snapshot.SegmentSize * snapshot.MaxArrays;
        }

        logger.LogInformation(
            "{PoolName}: Rented={Rented}, Returned={Returned}, Outstanding={Outstanding}, PeakInUse={PeakInUse}, PoolExhausted={PoolExhausted}, Dropped={Dropped}, Allocated={Allocated}, Trimmed={Trimmed}, TrimRemaining={TrimRemaining}, TrimInUse={TrimInUse}, StoreMiss={StoreMiss}",
            snapshot.PoolName,
            snapshot.Rented,
            snapshot.Returned,
            outstanding,
            snapshot.PeakInUse,
            snapshot.PoolExhausted,
            snapshot.Dropped,
            snapshot.Allocated,
            snapshot.Trimmed,
            snapshot.TrimRemaining,
            snapshot.TrimInUse,
            snapshot.StoreMiss);

        WriteLine(
            $"{snapshot.PoolName}: " +
            $"Attached={snapshot.Attached.ToString(CultureInfo.InvariantCulture)}, " +
            $"PoolId={snapshot.PoolId.ToString(CultureInfo.InvariantCulture)}, " +
            $"SegmentSize={snapshot.SegmentSize.ToString(CultureInfo.InvariantCulture)}, " +
            $"MaxArrays={snapshot.MaxArrays.ToString(CultureInfo.InvariantCulture)}, " +
            $"MaxBytes={maxBytes.ToString(CultureInfo.InvariantCulture)}, " +
            $"Rented={snapshot.Rented.ToString(CultureInfo.InvariantCulture)}, " +
            $"Returned={snapshot.Returned.ToString(CultureInfo.InvariantCulture)}, " +
            $"Outstanding={outstanding.ToString(CultureInfo.InvariantCulture)}, " +
            $"PeakInUse={snapshot.PeakInUse.ToString(CultureInfo.InvariantCulture)}, " +
            $"PoolExhausted={snapshot.PoolExhausted.ToString(CultureInfo.InvariantCulture)}, " +
            $"Dropped={snapshot.Dropped.ToString(CultureInfo.InvariantCulture)}, " +
            $"Allocated={snapshot.Allocated.ToString(CultureInfo.InvariantCulture)}, " +
            $"Trimmed={snapshot.Trimmed.ToString(CultureInfo.InvariantCulture)}, " +
            $"TrimRemaining={snapshot.TrimRemaining.ToString(CultureInfo.InvariantCulture)}, " +
            $"TrimInUse={snapshot.TrimInUse.ToString(CultureInfo.InvariantCulture)}, " +
            $"StoreMiss={snapshot.StoreMiss.ToString(CultureInfo.InvariantCulture)}, " +
            $"OtherEvents={snapshot.OtherEvents.ToString(CultureInfo.InvariantCulture)}, " +
            $"LastEvent={Quote(snapshot.LastEventName)}");
    }

    private PoolState GetState(string sourceName)
    {
        if (sourceName.Equals(FixedSizeArrayPoolSource, StringComparison.OrdinalIgnoreCase))
        {
            return _bufferPool;
        }

        if (sourceName.Equals(PathSegmentPoolSource, StringComparison.OrdinalIgnoreCase))
        {
            return _pathPool;
        }

        return _resultStorePool;
    }

    private static bool IsTargetSource(string sourceName)
        => sourceName.Equals(FixedSizeArrayPoolSource, StringComparison.OrdinalIgnoreCase) ||
           sourceName.Equals(PathSegmentPoolSource, StringComparison.OrdinalIgnoreCase) ||
           sourceName.Equals(ResultStorePoolSource, StringComparison.OrdinalIgnoreCase);

    private static bool TryReadInt(EventWrittenEventArgs eventData, int index, out int value)
    {
        value = 0;
        if (eventData.Payload is null || index < 0 || index >= eventData.Payload.Count)
        {
            return false;
        }

        try
        {
            value = Convert.ToInt32(eventData.Payload[index], CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadLong(EventWrittenEventArgs eventData, int index, out long value)
    {
        value = 0;
        if (eventData.Payload is null || index < 0 || index >= eventData.Payload.Count)
        {
            return false;
        }

        try
        {
            value = Convert.ToInt64(eventData.Payload[index], CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void BufferOrWriteLine(string line)
    {
        lock (_writeLock)
        {
            if (_writer is null)
            {
                _pendingLines.Add($"{DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)} {line}");
                return;
            }
        }

        WriteLine(line);
    }

    private void FlushPendingLines()
    {
        lock (_writeLock)
        {
            if (_writer is null)
            {
                return;
            }

            foreach (var line in _pendingLines)
            {
                _writer.WriteLine(line);
            }

            _pendingLines.Clear();
            _writer.Flush();
        }
    }

    private void WriteLine(string line)
    {
        lock (_writeLock)
        {
            if (_writer is null)
            {
                return;
            }

            _writer.Write(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            _writer.Write(' ');
            _writer.WriteLine(line);
            _writer.Flush();
        }
    }

    private string ResolveDiagnosticsDirectory()
    {
        var envPath = Environment.GetEnvironmentVariable("BENCH_DIAGNOSTICS_DIR");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return Path.GetFullPath(envPath);
        }

        var candidates = new[]
        {
            environment.ContentRootPath,
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."))
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(Path.Combine(fullPath, "gateway.far")))
            {
                return Path.Combine(fullPath, "diagnostics");
            }
        }

        return Path.Combine(Path.GetFullPath(environment.ContentRootPath), "diagnostics");
    }

    private void DisposeWriter()
    {
        lock (_writeLock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    public override void Dispose()
    {
        _snapshotCts?.Dispose();
        DisposeWriter();
        base.Dispose();
    }

    private static string Quote(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n")}\"";
    }

    private sealed class PoolState(string poolName, string sourceName)
    {
        public string PoolName { get; } = poolName;
        public string SourceName { get; } = sourceName;

        public bool Attached { get; set; }
        public int PoolId { get; set; }
        public int SegmentSize { get; set; }
        public int MaxArrays { get; set; }
        public long MaxBytes { get; set; }
        public int TrimRemaining { get; set; }
        public int TrimInUse { get; set; }
        public int PeakInUse { get; set; }

        public long Rented { get; set; }
        public long Returned { get; set; }
        public long PoolExhausted { get; set; }
        public long Dropped { get; set; }
        public long Allocated { get; set; }
        public long Trimmed { get; set; }
        public long StoreMiss { get; set; }
        public long OtherEvents { get; set; }
        public string LastEventName { get; set; } = "n/a";

        public PoolSnapshot Snapshot()
            => new(
                PoolName,
                SourceName,
                Attached,
                PoolId,
                SegmentSize,
                MaxArrays,
                MaxBytes,
                TrimRemaining,
                TrimInUse,
                PeakInUse,
                Rented,
                Returned,
                PoolExhausted,
                Dropped,
                Allocated,
                Trimmed,
                StoreMiss,
                OtherEvents,
                LastEventName);
    }

    private readonly record struct PoolSnapshot(
        string PoolName,
        string SourceName,
        bool Attached,
        int PoolId,
        int SegmentSize,
        int MaxArrays,
        long MaxBytes,
        int TrimRemaining,
        int TrimInUse,
        int PeakInUse,
        long Rented,
        long Returned,
        long PoolExhausted,
        long Dropped,
        long Allocated,
        long Trimmed,
        long StoreMiss,
        long OtherEvents,
        string LastEventName);
}
