using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Text;
using System.Threading.Channels;

namespace eShop.Reviews;

/// <summary>
/// Opt-in listener that records the arena/pool memory ledger to a CSV file for offline allocation
/// and leak ("escape") analysis. It activates only when the <c>FUSION_ARENA_TRACE</c> environment
/// variable holds an output file path. Formatted lines are enqueued to a channel and drained on a
/// single background task so the request path is barely perturbed.
/// </summary>
internal sealed class MemoryArenaEventListener : EventListener, IHostedService
{
    private const string EnvironmentVariable = "FUSION_ARENA_TRACE";

    // The three buffer event sources we tap, and the level each is enabled at:
    //   arena (HotChocolate-Buffers-MemoryArena)        @ Verbose       - full per-arena lifecycle
    //   pool  (HotChocolate-Buffers-FixedSizeArrayPool) @ Informational - the byte[] churn signal
    //         (BufferAllocated / BufferDropped / PoolTrimmed / PoolExhausted). Their per-buffer
    //         BufferRented / BufferReturned are Verbose, so Informational deliberately skips that
    //         firehose (millions of rows) while keeping the fresh-allocation cadence.
    //   json  (HotChocolate-Buffers-JsonMemory)         @ Verbose       - per-arena page batch
    //         rent / return / abandon (low volume: one event per arena seal/abandon).
    private static readonly Dictionary<string, (string Tag, EventLevel Level)> s_taps =
        new(StringComparer.Ordinal)
        {
            ["HotChocolate-Buffers-MemoryArena"] = ("arena", EventLevel.Verbose),
            ["HotChocolate-Buffers-FixedSizeArrayPool"] = ("pool", EventLevel.Informational),
            ["HotChocolate-Buffers-JsonMemory"] = ("json", EventLevel.Verbose)
        };

    // CSV columns: relMs,source,event,payload
    //   relMs   = milliseconds since the listener started.
    //   source  = arena | pool | json (see s_taps).
    //   event   = the EventSource method name, e.g. ArenaCreated, MemoryRented, ArrayRented,
    //             ArrayGrown, ArenaSealed, ArenaAbandoned, BufferAllocated, BufferDropped,
    //             PoolTrimmed, PoolExhausted, BufferReturned, BufferAbandoned.
    //   payload = name=value pairs joined by '|', e.g. arenaId=12|sizeInBytes=131072.
    //
    // Escape-analysis cheat sheet:
    //   - byte[] page churn = count of pool/BufferAllocated (fresh new byte[131072]) plus
    //     pool/BufferDropped (returned but the pool was full) in steady state. pool/PoolTrimmed
    //     shows the 1-minute trim timer releasing warm pages; pool/PoolExhausted is the hard ceiling.
    //   - per-arena page leak check: for one arenaId, count(arena/MemoryRented) must equal
    //     arena/ArenaSealed.pagesReturned (clean) or arena/ArenaAbandoned.pagesAbandoned (leaked).
    //   - arena/ArrayGrown is BOTH a return of the old array (oldLength) AND a rent of the new array
    //     (newLength). Reconcile arrays as (rentArr + Sum growArr.newLength) vs
    //     (Sum growArr.oldLength + sealed/abandoned arrays); a naive rentArr-vs-returned overcounts.
    private const string Header = "relMs,source,event,payload";

    private readonly string? _path;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<EventSource> _seen = [];
    private readonly Channel<string>? _channel;
    private readonly EventHandler _processExitHandler;

    private Task? _drainTask;
    private int _closed;

    public MemoryArenaEventListener()
    {
        _path = Environment.GetEnvironmentVariable(EnvironmentVariable);
        _processExitHandler = (_, _) => Shutdown();

        if (!string.IsNullOrWhiteSpace(_path))
        {
            _channel = Channel.CreateUnbounded<string>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });

            // Sources reported via the base ctor's OnEventSourceCreated while _channel was still
            // null (they existed before this ctor body ran) are enabled now.
            foreach (var source in _seen)
            {
                Enable(source);
            }
        }
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (!s_taps.ContainsKey(eventSource.Name))
        {
            return;
        }

        _seen.Add(eventSource);

        if (_channel is not null)
        {
            Enable(eventSource);
        }
    }

    private void Enable(EventSource eventSource)
    {
        if (s_taps.TryGetValue(eventSource.Name, out var tap))
        {
            EnableEvents(eventSource, tap.Level, EventKeywords.All);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        var channel = _channel;
        if (channel is null)
        {
            return;
        }

        var sourceName = e.EventSource?.Name;
        if (sourceName is null || !s_taps.TryGetValue(sourceName, out var tap))
        {
            return;
        }

        var relMs = _clock.ElapsedMilliseconds;
        var name = e.EventName ?? e.EventId.ToString(CultureInfo.InvariantCulture);

        var sb = new StringBuilder(64);
        sb.Append(relMs.ToString(CultureInfo.InvariantCulture))
            .Append(',').Append(tap.Tag)
            .Append(',').Append(name)
            .Append(',');

        var payload = e.Payload;
        var names = e.PayloadNames;
        if (payload is not null && names is not null)
        {
            var count = Math.Min(payload.Count, names.Count);
            for (var i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    sb.Append('|');
                }

                sb.Append(names[i]).Append('=')
                    .Append(Convert.ToString(payload[i], CultureInfo.InvariantCulture));
            }
        }

        channel.Writer.TryWrite(sb.ToString());
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return Task.CompletedTask;
        }

        var path = Path.GetFullPath(_path!);
        Console.WriteLine($"[MemoryArena] Tracing enabled -> {path}");

        AppDomain.CurrentDomain.ProcessExit += _processExitHandler;
        _drainTask = Task.Run(() => DrainAsync(path));
        return Task.CompletedTask;
    }

    private async Task DrainAsync(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var writer =
            new StreamWriter(path, append: false, Encoding.UTF8) { AutoFlush = false };
        await writer.WriteLineAsync(Header).ConfigureAwait(false);

        var reader = _channel!.Reader;
        while (await reader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (reader.TryRead(out var line))
            {
                await writer.WriteLineAsync(line).ConfigureAwait(false);
            }

            await writer.FlushAsync().ConfigureAwait(false);
        }

        await writer.FlushAsync().ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Shutdown();

        var drainTask = _drainTask;
        if (drainTask is not null)
        {
            await drainTask.ConfigureAwait(false);
        }
    }

    private void Shutdown()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
        {
            return;
        }

        AppDomain.CurrentDomain.ProcessExit -= _processExitHandler;
        _channel?.Writer.TryComplete();
    }

    public override void Dispose()
    {
        Shutdown();
        base.Dispose();
    }
}
