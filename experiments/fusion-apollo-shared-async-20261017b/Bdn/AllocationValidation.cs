using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApolloExecutorBdn;

internal static class AllocationValidation
{
    internal static async Task RunAsync(string output)
    {
        foreach (var shape in new[] { "ordinary", "async-single9", "async-batch9" })
        {
            var fixture = new ExecutorBenchmarks { Shape = shape };
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
                    var noGc = GC.TryStartNoGCRegion(512L * 1024 * 1024, disallowFullBlockingGC: true);
                    if (!noGc)
                    {
                        throw new InvalidOperationException("NoGC region unavailable.");
                    }
                    string? error = null;
                    string? endError = null;
                    var beforeCalibration = default(Calibration);
                    var afterCalibration = default(Calibration);
                    var collections = new int[3];
                    var completed = fixture.CompletedOperations;
                    long bytes = 0;
                    double elapsed = 0;
                    var mode = string.Empty;
                    try
                    {
                        beforeCalibration = Calibrate();
                        var gen0 = GC.CollectionCount(0);
                        var gen1 = GC.CollectionCount(1);
                        var gen2 = GC.CollectionCount(2);
                        var before = GC.GetTotalAllocatedBytes(precise: true);
                        var start = Stopwatch.GetTimestamp();
                        for (var i = 0; i < 1000; i++)
                        {
                            await fixture.ExecuteOneAsync().ConfigureAwait(false);
                        }
                        elapsed = Stopwatch.GetElapsedTime(start).TotalSeconds;
                        bytes = GC.GetTotalAllocatedBytes(precise: true) - before;
                        afterCalibration = Calibrate();
                        collections[0] = GC.CollectionCount(0) - gen0;
                        collections[1] = GC.CollectionCount(1) - gen1;
                        collections[2] = GC.CollectionCount(2) - gen2;
                        mode = GCSettings.LatencyMode.ToString();
                    }
                    catch (Exception exception)
                    {
                        error = exception.ToString();
                    }
                    finally
                    {
                        try
                        {
                            GC.EndNoGCRegion();
                        }
                        catch (InvalidOperationException exception)
                        {
                            endError = exception.Message;
                        }
                    }
                    var idleAfter = ObserveIdle();
                    completed = fixture.CompletedOperations - completed;
                    var valid = error is null && endError is null && completed == 1000
                        && beforeCalibration.Process == 523240 && beforeCalibration.Thread == 523240
                        && afterCalibration.Process == 523240 && afterCalibration.Thread == 523240
                        && collections.All(count => count == 0) && bytes > 0
                        && mode == nameof(GCLatencyMode.NoGCRegion);
                    var row = new
                    {
                        shape, block, operations = 1000, completed, processBytes = bytes,
                        bytesPerOperation = bytes / 1000d, elapsed, beforeCalibration, afterCalibration,
                        idleBefore, idleAfter, collections, mode, error, endError, valid
                    };
                    var json = JsonSerializer.Serialize(row);
                    File.AppendAllText(System.IO.Path.Combine(output, "allocation.jsonl"), json + "\n");
                    Console.Error.WriteLine(json);
                    if (!valid)
                    {
                        throw new InvalidOperationException("Invalid allocation region; no retry.");
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
            GC.KeepAlive(new byte[4096]);
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
