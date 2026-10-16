using System.Diagnostics;
using HotChocolate.Fusion.Configuration;

ThreadPool.SetMinThreads(256, 256);
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("fusion").RemoveAllLoggers()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { MaxConnectionsPerServer = 256 });
builder.AddGraphQLGateway(disableDefaultSecurity: true)
    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
    .AddFileSystemConfiguration("./gateway.far");
var app = builder.Build();
app.MapGraphQLHttp().WithOptions(o => o.MaxBatchSize = 25000);
app.MapGet("/perf/state", () =>
{
    using var p = Process.GetCurrentProcess();
    var gc = GC.GetGCMemoryInfo();
    return Results.Json(new {
        timestamp = Stopwatch.GetTimestamp(), frequency = Stopwatch.Frequency,
        utc = DateTimeOffset.UtcNow, cpuMs = p.TotalProcessorTime.TotalMilliseconds,
        allocated = GC.GetTotalAllocatedBytes(precise: true),
        gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2),
        pauseMs = GC.GetTotalPauseDuration().TotalMilliseconds,
        heap = gc.HeapSizeBytes, fragmented = gc.FragmentedBytes, committed = gc.TotalCommittedBytes,
        workingSet = p.WorkingSet64, peakWorkingSet = p.PeakWorkingSet64,
        serverGC = System.Runtime.GCSettings.IsServerGC,
        runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
    });
});
app.Run();
