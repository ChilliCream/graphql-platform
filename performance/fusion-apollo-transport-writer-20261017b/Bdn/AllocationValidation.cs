using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApolloAliasBdn;

internal static class AllocationValidation
{
    internal static async Task RunAsync(string output)
    {
        foreach (var shape in new[] { "ordinary", "alias2", "alias-sequence10" })
        {
            var fixture = new AliasBenchmarks { Shape = shape };
            try
            {
                await fixture.InitializeAsync(warm: true);
                for (var block = 0; block < 3; block++)
                {
                    Console.Error.WriteLine(JsonSerializer.Serialize(new { shape, block, state = "starting" }));
                    var idleBefore = ObserveIdle();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    var started = GC.TryStartNoGCRegion(512L * 1024 * 1024, disallowFullBlockingGC: true);
                    if (!started)
                    {
                        throw new InvalidOperationException("NoGC region unavailable.");
                    }
                    try
                    {
                        var calibrationBefore = Calibrate();
                        var gen0 = GC.CollectionCount(0);
                        var gen1 = GC.CollectionCount(1);
                        var gen2 = GC.CollectionCount(2);
                        var thread = Environment.CurrentManagedThreadId;
                        var synchronous = true;
                        var completedBefore = fixture.CompletedOperations;
                        var threadBefore = GC.GetAllocatedBytesForCurrentThread();
                        var before = GC.GetTotalAllocatedBytes(precise: true);
                        var start = Stopwatch.GetTimestamp();
                        for (var i = 0; i < 1000; i++)
                        {
                            var operation = fixture.ExecuteOneAsync();
                            synchronous &= operation.IsCompletedSuccessfully;
                            await operation.ConfigureAwait(false);
                        }
                        var elapsed = Stopwatch.GetElapsedTime(start).TotalSeconds;
                        var bytes = GC.GetTotalAllocatedBytes(precise: true) - before;
                        var threadBytes = GC.GetAllocatedBytesForCurrentThread() - threadBefore;
                        var sameThread = thread == Environment.CurrentManagedThreadId;
                        var calibrationAfter = Calibrate();
                        var collections = new[]
                        {
                            GC.CollectionCount(0) - gen0,
                            GC.CollectionCount(1) - gen1,
                            GC.CollectionCount(2) - gen2
                        };
                        var mode = GCSettings.LatencyMode.ToString();
                        string? endError = null;
                        try
                        {
                            GC.EndNoGCRegion();
                        }
                        catch (InvalidOperationException exception)
                        {
                            endError = exception.Message;
                        }
                        var idleAfter = ObserveIdle();
                        var valid = calibrationBefore.Process == 523240 && calibrationBefore.Thread == 523240
                            && calibrationAfter.Process == 523240 && calibrationAfter.Thread == 523240
                            && collections.All(count => count == 0) && endError is null
                            && mode == nameof(GCLatencyMode.NoGCRegion)
                            && fixture.CompletedOperations - completedBefore == 1000;
                        var row = new
                        {
                            shape, block, operations = fixture.CompletedOperations - completedBefore,
                            processBytes = bytes, threadBytes,
                            bytesPerOperation = bytes / 1000d, synchronous, sameThread, elapsed,
                            calibrationBefore, calibrationAfter, idleBefore, idleAfter,
                            collections, mode, endError, valid
                        };
                        var json = JsonSerializer.Serialize(row);
                        File.AppendAllText(System.IO.Path.Combine(output, "allocation.jsonl"), json + "\n");
                        Console.Error.WriteLine(json);
                        if (!valid)
                        {
                            throw new InvalidOperationException("Invalid allocation region; no retry.");
                        }
                    }
                    finally
                    {
                        if (GCSettings.LatencyMode is GCLatencyMode.NoGCRegion)
                        {
                            GC.EndNoGCRegion();
                        }
                    }
                }
            }
            finally
            {
                await fixture.CleanupAsync();
            }
        }
    }

    private readonly record struct Calibration(long Process, long Thread);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Calibration Calibrate()
    {
        var thread = GC.GetAllocatedBytesForCurrentThread();
        var process = GC.GetTotalAllocatedBytes(precise: true);
        for (var i = 0; i < 127; i++)
        {
            var value = new byte[4096];
            value[0] = (byte)i;
            GC.KeepAlive(value);
        }
        return new Calibration(GC.GetTotalAllocatedBytes(precise: true) - process,
            GC.GetAllocatedBytesForCurrentThread() - thread);
    }

    private static object ObserveIdle()
    {
        var before = GC.GetTotalAllocatedBytes(precise: true);
        var start = Stopwatch.GetTimestamp();
        Thread.Sleep(50);
        return new { bytes = GC.GetTotalAllocatedBytes(precise: true) - before,
            seconds = Stopwatch.GetElapsedTime(start).TotalSeconds };
    }
}
