// PostgreSQL Transport Benchmark
//
// Creates two independent Mocha bus instances (Service A and Service B) sharing
// the same PostgreSQL database. Measures throughput and latency for all three
// messaging patterns:
//
//   1. Publish / Subscribe   (fan-out event)
//   2. Send                  (point-to-point command)
//   3. Request / Reply       (round-trip)
//
// Usage:
//   dotnet run                                               # uses default localhost connection
//   dotnet run -- "Host=localhost;Database=mocha_bench;..."   # explicit connection string
//   BENCHMARK_CONNECTION_STRING="..." dotnet run              # via environment variable

using System.Collections.Concurrent;
using System.Diagnostics;
using Mocha;
using Mocha.Hosting;
using Mocha.Transport.Postgres;

const int warmupCount = 100;
const int messageCount = 10_000;
const int requestCount = 500;
const int producerConcurrency = 50;
var timeout = TimeSpan.FromSeconds(120);

// ═══════════════════════════════════════════════════════════════════════════
//  PostgreSQL connection
// ═══════════════════════════════════════════════════════════════════════════

var connectionString = args.FirstOrDefault()
    ?? Environment.GetEnvironmentVariable("BENCHMARK_CONNECTION_STRING")
    ?? "Host=localhost;Database=mocha_bench;Username=postgres;Password=postgres";

// ═══════════════════════════════════════════════════════════════════════════
//  Banner
// ═══════════════════════════════════════════════════════════════════════════

Console.WriteLine();
Console.WriteLine("  PostgreSQL Transport Benchmark");
Console.WriteLine("  ══════════════════════════════════════════════════════");
Console.WriteLine($"  Connection : {MaskPassword(connectionString)}");
Console.WriteLine($"  Messages   : {messageCount:N0} (publish & send)");
Console.WriteLine($"  Concurrency: {producerConcurrency} producers");
Console.WriteLine($"  Request    : {requestCount:N0} round-trips");
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════
//  Service B — Consumer (start first so topology is provisioned)
// ═══════════════════════════════════════════════════════════════════════════

var collector = new LatencyCollector();

Console.Write("  Starting Service B (consumer) …");

var consumerApp = WebApplication.CreateSlimBuilder().Build();
{
    var b = WebApplication.CreateSlimBuilder();
    b.WebHost.UseUrls("http://localhost:5101");
    b.Services.AddSingleton(collector);
    b.Services
        .AddMessageBus()
        .AddEventHandler<BenchmarkEventHandler>()
        .AddEventHandler<BenchmarkCommandHandler>()
        .AddRequestHandler<BenchmarkRequestHandler>()
        .AddPostgres(t =>
        {
            t.ConnectionString(connectionString);
            t.ConfigureDefaults(p =>
            {
                p.Endpoint.MaxBatchSize = 1000;
                p.Endpoint.MaxConcurrency = 1000;
            });

            // Explicit queue for the Send pattern
            t.DeclareQueue("bench-cmd");
            t.Endpoint("bench-cmd-ep")
                .Queue("bench-cmd")
                .Handler<BenchmarkCommandHandler>();
        });
    consumerApp = b.Build();
}

await consumerApp.StartAsync();
Console.WriteLine(" done");

// Give the consumer time to provision topology and start polling.
await Task.Delay(3_000);

// ═══════════════════════════════════════════════════════════════════════════
//  Service A — Producer
// ═══════════════════════════════════════════════════════════════════════════

Console.Write("  Starting Service A (producer) …");

var producerApp = WebApplication.CreateSlimBuilder().Build();
{
    var b = WebApplication.CreateSlimBuilder();
    b.WebHost.UseUrls("http://localhost:5102");
    b.Services
        .AddMessageBus()
        .AddPostgres(t =>
        {
            t.ConnectionString(connectionString);

            // Dispatch endpoint so SendAsync routes BenchmarkCommand to the queue
            t.DispatchEndpoint("bench-cmd-dispatch")
                .ToQueue("bench-cmd")
                .Send<BenchmarkCommand>();
        });
    producerApp = b.Build();
}

await producerApp.StartAsync();
Console.WriteLine(" done");
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════
//  Warm-up
// ═══════════════════════════════════════════════════════════════════════════

Console.Write("  Warming up …");
collector.Reset(warmupCount);
{
    using var scope = producerApp.Services.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
    for (var i = 0; i < warmupCount; i++)
    {
        await bus.PublishAsync(
            new BenchmarkEvent { SentTicks = Stopwatch.GetTimestamp(), Sequence = i },
            CancellationToken.None);
    }
}

await collector.WaitAsync(timeout);
Console.WriteLine(" done");
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════
//  Scenario 1 — Publish / Subscribe
// ═══════════════════════════════════════════════════════════════════════════

Console.WriteLine("  ── Publish / Subscribe ──────────────────────────────");
collector.Reset(messageCount);
var sw = Stopwatch.StartNew();
{
    using var scope = producerApp.Services.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
    await Parallel.ForEachAsync(
        Enumerable.Range(0, messageCount),
        new ParallelOptions { MaxDegreeOfParallelism = producerConcurrency },
        async (i, ct) =>
        {
            await bus.PublishAsync(
                new BenchmarkEvent { SentTicks = Stopwatch.GetTimestamp(), Sequence = i }, ct);
        });
}

await collector.WaitAsync(timeout);
sw.Stop();
PrintResults("Latency", messageCount, sw.Elapsed, collector.GetSortedSamples());

// ═══════════════════════════════════════════════════════════════════════════
//  Scenario 2 — Send (point-to-point)
// ═══════════════════════════════════════════════════════════════════════════

Console.WriteLine("  ── Send (point-to-point) ───────────────────────────");
collector.Reset(messageCount);
sw = Stopwatch.StartNew();
{
    using var scope = producerApp.Services.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
    await Parallel.ForEachAsync(
        Enumerable.Range(0, messageCount),
        new ParallelOptions { MaxDegreeOfParallelism = producerConcurrency },
        async (i, ct) =>
        {
            await bus.SendAsync(
                new BenchmarkCommand { SentTicks = Stopwatch.GetTimestamp(), Sequence = i }, ct);
        });
}

await collector.WaitAsync(timeout);
sw.Stop();
PrintResults("Latency", messageCount, sw.Elapsed, collector.GetSortedSamples());

// ═══════════════════════════════════════════════════════════════════════════
//  Scenario 3 — Request / Reply
// ═══════════════════════════════════════════════════════════════════════════

Console.WriteLine("  ── Request / Reply ─────────────────────────────────");
var requestSamples = new ConcurrentBag<double>();
sw = Stopwatch.StartNew();
{
    // Run requests with moderate concurrency to measure realistic throughput.
    const int concurrency = 10;
    const int perWorker = requestCount / concurrency;

    var tasks = Enumerable.Range(0, concurrency).Select(async w =>
    {
        // Last worker picks up the remainder.
        var count = w < concurrency - 1 ? perWorker : requestCount - perWorker * (concurrency - 1);

        using var scope = producerApp.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        for (var i = 0; i < count; i++)
        {
            var start = Stopwatch.GetTimestamp();
            await bus.RequestAsync(
                new BenchmarkRequest { Sequence = i },
                CancellationToken.None);
            requestSamples.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
    });

    await Task.WhenAll(tasks);
}

sw.Stop();
PrintResults("Round-trip", requestCount, sw.Elapsed, [.. requestSamples.Order()]);

// ═══════════════════════════════════════════════════════════════════════════
//  Shutdown
// ═══════════════════════════════════════════════════════════════════════════

Console.WriteLine("  Shutting down …");
await producerApp.StopAsync();
await consumerApp.StopAsync();
Console.WriteLine("  Done.");
Console.WriteLine();

return;

// ═══════════════════════════════════════════════════════════════════════════
//  Helpers
// ═══════════════════════════════════════════════════════════════════════════

static void PrintResults(string label, int count, TimeSpan duration, double[] samples)
{
    var throughput = count / duration.TotalSeconds;

    Console.WriteLine($"     Messages  : {count:N0}");
    Console.WriteLine($"     Duration  : {duration.TotalMilliseconds:N0} ms");
    Console.WriteLine($"     Throughput: {throughput:N1} msg/s");
    Console.WriteLine($"     {label}:");
    Console.WriteLine($"       Min : {samples[0]:N2} ms");
    Console.WriteLine($"       P50 : {Percentile(samples, 0.50):N2} ms");
    Console.WriteLine($"       P95 : {Percentile(samples, 0.95):N2} ms");
    Console.WriteLine($"       P99 : {Percentile(samples, 0.99):N2} ms");
    Console.WriteLine($"       Max : {samples[^1]:N2} ms");
    Console.WriteLine();
}

static double Percentile(double[] sorted, double p)
{
    var idx = (int)Math.Ceiling(p * sorted.Length) - 1;
    return sorted[Math.Clamp(idx, 0, sorted.Length - 1)];
}

static string MaskPassword(string cs)
{
    var parts = cs.Split(';');
    for (var i = 0; i < parts.Length; i++)
    {
        if (parts[i].TrimStart().StartsWith("Password", StringComparison.OrdinalIgnoreCase))
        {
            parts[i] = parts[i][..parts[i].IndexOf('=')] + "=***";
        }
    }

    return string.Join(';', parts);
}

// ═══════════════════════════════════════════════════════════════════════════
//  Messages
// ═══════════════════════════════════════════════════════════════════════════

public sealed class BenchmarkEvent
{
    public required long SentTicks { get; init; }
    public required int Sequence { get; init; }
}

public sealed class BenchmarkCommand
{
    public required long SentTicks { get; init; }
    public required int Sequence { get; init; }
}

public sealed class BenchmarkRequest : IEventRequest<BenchmarkResponse>
{
    public required int Sequence { get; init; }
}

public sealed class BenchmarkResponse
{
    public required string Status { get; init; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Handlers (Service B)
// ═══════════════════════════════════════════════════════════════════════════

public sealed class BenchmarkEventHandler(LatencyCollector collector)
    : IEventHandler<BenchmarkEvent>
{
    public ValueTask HandleAsync(BenchmarkEvent message, CancellationToken cancellationToken)
    {
        collector.Record(message.SentTicks);
        return ValueTask.CompletedTask;
    }
}

public sealed class BenchmarkCommandHandler(LatencyCollector collector)
    : IEventHandler<BenchmarkCommand>
{
    public ValueTask HandleAsync(BenchmarkCommand message, CancellationToken cancellationToken)
    {
        collector.Record(message.SentTicks);
        return ValueTask.CompletedTask;
    }
}

public sealed class BenchmarkRequestHandler
    : IEventRequestHandler<BenchmarkRequest, BenchmarkResponse>
{
    public ValueTask<BenchmarkResponse> HandleAsync(
        BenchmarkRequest request,
        CancellationToken cancellationToken)
    {
        return new(new BenchmarkResponse { Status = "ok" });
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Latency collector
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Thread-safe collector that records handler latency samples and signals
/// when the expected number of messages have been received.
/// </summary>
public sealed class LatencyCollector
{
    private ConcurrentQueue<double> _samples = new();
    private int _received;
    private int _expected;
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Reset(int expectedCount)
    {
        _samples = new ConcurrentQueue<double>();
        Volatile.Write(ref _received, 0);
        _expected = expectedCount;
        _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void Record(long sentTimestamp)
    {
        var latencyMs = Stopwatch.GetElapsedTime(sentTimestamp).TotalMilliseconds;
        _samples.Enqueue(latencyMs);

        if (Interlocked.Increment(ref _received) >= _expected)
        {
            _tcs.TrySetResult();
        }
    }

    public async Task WaitAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            await _tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            var received = Volatile.Read(ref _received);
            throw new TimeoutException(
                $"Timed out waiting for messages. Received {received} of {_expected}.");
        }
    }

    public double[] GetSortedSamples() => [.. _samples.Order()];
}
