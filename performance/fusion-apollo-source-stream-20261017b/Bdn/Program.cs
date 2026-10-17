using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Transport.Http;
using Perfolizer.Mathematics.OutlierDetection;

namespace ApolloSourceStreamBdn;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var enginePath = typeof(GraphQLHttpResponse).Assembly.Location;
        var identity = new
        {
            runtime = RuntimeInformation.FrameworkDescription,
            GCSettings.IsServerGC,
            gcConfiguration = GC.GetConfigurationVariables(),
            executionPath = enginePath,
            executionDllSha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(enginePath)))
        };
        var allocationMode = args[0] == "allocate";
        if (identity.runtime != ".NET 10.0.12" || !identity.IsServerGC
            || identity.gcConfiguration["GCDynamicAdaptationMode"] is not long adaptationMode
            || adaptationMode != (allocationMode ? 0L : 1L)
            || (allocationMode && identity.gcConfiguration["HeapCount"] is not 4L))
        {
            throw new InvalidOperationException("Unexpected runtime or GC policy.");
        }

        Directory.CreateDirectory(args[1]);
        File.WriteAllText(Path.Combine(args[1], "runtime-identity.json"), JsonSerializer.Serialize(identity));

        if (args[0] is "verify" or "allocate")
        {
            var observations = new List<object>();
            foreach (var shape in new[] { "known128", "unknown4096", "unknown32768" })
            {
                var fixture = new SourceResponseBenchmarks { Shape = shape };
                await fixture.InitializeAsync(warm: args[0] == "allocate");
                if (args[0] == "allocate")
                {
                    observations.Add(fixture.MeasureAllocation(args[1]));
                }
            }
            if (args[0] == "allocate")
            {
                File.WriteAllText(Path.Combine(args[1], "allocation.json"), JsonSerializer.Serialize(observations));
            }
            return 0;
        }
        if (args[0] != "benchmark")
        {
            throw new ArgumentException("Expected verify, allocate or benchmark.");
        }

        var job = Job.Default
            .WithToolchain(new InProcessEmitToolchain(TimeSpan.FromSeconds(60), logOutput: true))
            .WithLaunchCount(1)
            .WithWarmupCount(3)
            .WithIterationCount(12)
            .WithInvocationCount(1)
            .WithUnrollFactor(1)
            .WithGcForce(false)
            .WithOutlierMode(OutlierMode.DontRemove)
            .WithId("DefaultGC-1000SourceResponses");
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(job)
            .AddExporter(JsonExporter.Full)
            .WithArtifactsPath(args[1]);
        var summary = BenchmarkRunner.Run<SourceResponseBenchmarks>(config);
        return summary.HasCriticalValidationErrors || summary.Reports.Length != 3
            || summary.Reports.Any(report => !report.Success) ? 1 : 0;
    }
}

public class SourceResponseBenchmarks
{
    private byte[] _payload = null!;
    private bool _knownLength;
    private long _completed;

    [Params("known128", "unknown4096", "unknown32768")]
    public string Shape { get; set; } = "known128";

    [GlobalSetup]
    public Task SetupAsync() => InitializeAsync(warm: true);

    public async Task InitializeAsync(bool warm)
    {
        _knownLength = Shape == "known128";
        var length = Shape switch
        {
            "known128" => 128,
            "unknown4096" => 4096,
            "unknown32768" => 32768,
            _ => throw new InvalidOperationException("Unexpected shape.")
        };
        const string prefix = "{\"data\":{\"id\":7,\"value\":\"";
        const string suffix = "\"}}";
        _payload = Encoding.UTF8.GetBytes(prefix + new string('a', length - prefix.Length - suffix.Length) + suffix);
        var stream = new PayloadStream(_payload);
        using (var response = CreateResponse(stream))
        using (var arena = new MemoryArena())
        {
            using var document = await response.ReadAsResultAsync(arena);
            arena.Seal();
            if (document.Root.GetRawText() != Encoding.UTF8.GetString(_payload)
                || stream.BytesRead != _payload.Length || !stream.IsDisposed)
            {
                throw new InvalidOperationException("Incorrect payload or stream ownership.");
            }
        }
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Shape,
            payloadLength = _payload.Length,
            payloadSha256 = Convert.ToHexStringLower(SHA256.HashData(_payload)),
            stream.ReadCalls,
            stream.ZeroByteReads,
            stream.BytesRead,
            stream.IsDisposed
        }));
        if (warm)
        {
            var started = Stopwatch.GetTimestamp();
            do
            {
                await ExecuteOneAsync();
            }
            while (Stopwatch.GetElapsedTime(started).TotalSeconds < 3);
        }
    }

    [Benchmark(OperationsPerInvoke = 1000)]
    public async Task ReadAsync()
    {
        var before = _completed;
        for (var i = 0; i < 1000; i++)
        {
            await ExecuteOneAsync();
        }
        if (_completed - before != 1000)
        {
            throw new InvalidOperationException("Incomplete benchmark invocation.");
        }
    }

    public object MeasureAllocation(string outputDirectory)
    {
        var blocks = new List<object>();
        for (var block = 0; block < 5; block++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var started = GC.TryStartNoGCRegion(512 * 1024 * 1024, disallowFullBlockingGC: false);
            if (!started)
            {
                throw new InvalidOperationException("The controlled allocation region could not start.");
            }
            var gcBefore = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
            var calibrationBefore = Calibrate();
            var thread = Environment.CurrentManagedThreadId;
            var start = GC.GetAllocatedBytesForCurrentThread();
            var completed = _completed;
            var synchronous = true;
            for (var i = 0; i < 1000; i++)
            {
                var pending = ExecuteOneAsync();
                if (!pending.IsCompletedSuccessfully)
                {
                    synchronous = false;
                }
                pending.GetAwaiter().GetResult();
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            var endThread = Environment.CurrentManagedThreadId;
            var calibrationAfter = Calibrate();
            var gcAfter = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
            var modeBeforeEnd = GCSettings.LatencyMode;
            string? endError = null;
            try
            {
                GC.EndNoGCRegion();
            }
            catch (InvalidOperationException exception)
            {
                endError = exception.Message;
            }
            var valid = synchronous && thread == endThread && _completed - completed == 1000
                && calibrationBefore == 523240 && calibrationAfter == 523240
                && gcBefore.SequenceEqual(gcAfter) && modeBeforeEnd == GCLatencyMode.NoGCRegion
                && endError is null;
            blocks.Add(new
            {
                block, allocated, operations = _completed - completed, synchronous, thread, endThread,
                calibrationBefore, calibrationAfter, gcBefore, gcAfter, modeBeforeEnd, endError, valid
            });
            File.WriteAllText(Path.Combine(outputDirectory, $"allocation-{Shape}.json"), JsonSerializer.Serialize(blocks));
            if (!valid)
            {
                throw new InvalidOperationException("Invalid scoped allocation accounting.");
            }
        }
        return new { Shape, method = "Synchronous production path; fixed four heaps, 512MiB no-GC region, current-thread counter and exact known-allocation checks", blocks };
    }

    private async ValueTask ExecuteOneAsync()
    {
        using var response = CreateResponse(new PayloadStream(_payload));
        using var arena = new MemoryArena();
        try
        {
            using var document = await response.ReadAsResultAsync(arena).ConfigureAwait(false);
            if (document.Root.GetProperty("data"u8).GetProperty("id"u8).GetInt32() != 7)
            {
                throw new InvalidOperationException("Incorrect result.");
            }
        }
        finally
        {
            arena.Seal();
        }
        _completed++;
    }

    private GraphQLHttpResponse CreateResponse(PayloadStream stream)
    {
        var message = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
        };
        message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/graphql-response+json");
        message.Content.Headers.ContentLength = _knownLength ? _payload.Length : null;
        return new GraphQLHttpResponse(message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Calibrate()
    {
        var start = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 127; i++)
        {
            GC.KeepAlive(new byte[4096]);
        }
        return GC.GetAllocatedBytesForCurrentThread() - start;
    }

    private sealed class PayloadStream(byte[] payload) : Stream
    {
        public int BytesRead { get; private set; }
        public int ReadCalls { get; private set; }
        public int ZeroByteReads { get; private set; }
        public bool IsDisposed { get; private set; }
        public override bool CanRead => !IsDisposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadCalls++;
            if (buffer.Length == 0)
            {
                ZeroByteReads++;
            }
            var count = Math.Min(buffer.Length, payload.Length - BytesRead);
            payload.AsMemory(BytesRead, count).CopyTo(buffer);
            BytesRead += count;
            return ValueTask.FromResult(count);
        }
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
