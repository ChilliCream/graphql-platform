using System.Diagnostics.Tracing;
using System.Threading.Channels;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("Fusion");

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far")
    .ModifyRequestOptions(o =>
    {
        o.CollectOperationPlanTelemetry = true;
        o.IncludeExceptionDetails = true;
    });
// .AddDiagnosticEventListener(_ => new ErrorCollector());

var app = builder.Build();

// Start memory pool monitoring
// var memoryPoolCollector = new MemoryPoolCollector();
// var metaDbCollector = new MetaDbCollector();

#if RELEASE
app.MapGraphQLHttp();
#else
app.MapGraphQL().WithOptions(new GraphQLServerOptions { Tool = { ServeMode = GraphQLToolServeMode.Insider } });
#endif

app.Run();

public class ErrorCollector : FusionExecutionDiagnosticEventListener
{
    private readonly ChannelWriter<ErrorInfo> _writer;

    public ErrorCollector()
    {
        var channel = Channel.CreateUnbounded<ErrorInfo>();
        _writer = channel.Writer;

        WriteErrorsAsync(channel.Reader).FireAndForget();
    }

    private async Task WriteErrorsAsync(ChannelReader<ErrorInfo> reader)
    {
        await using var stream = File.Create("/Users/michael/local/hc-4/src/HotChocolate/Fusion-vnext/benchmarks/k6/eShop.Gateway/error.log");
        await using var errorLog = new StreamWriter(stream);

        await foreach (var error in reader.ReadAllAsync())
        {
            await errorLog.WriteLineAsync("-------");
            await errorLog.WriteLineAsync(error.Area);
            await errorLog.WriteLineAsync(error.Message);

            if (error.StackTrace is not null)
            {
                await errorLog.WriteLineAsync(error.StackTrace);
            }

            await errorLog.FlushAsync();
        }
    }

    public override void RequestError(RequestContext context, Exception error)
    {
        _writer.TryWrite(new ErrorInfo("RequestError", error.Message, error.StackTrace));
    }

    public override void RequestError(RequestContext context, IError error)
    {
        _writer.TryWrite(new ErrorInfo("RequestError", error.Message, error.Exception?.StackTrace));
    }

    public override void ValidationErrors(RequestContext context, IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            _writer.TryWrite(new ErrorInfo("ValidationError", error.Message, error.Exception?.StackTrace));
        }
    }

    public override void ExecutionNodeError(OperationPlanContext context, ExecutionNode node, Exception error)
    {
        _writer.TryWrite(new ErrorInfo($"ExecutionNodeError (Node: {node.Id})", error.Message, error.StackTrace));
    }

    public override void PlanOperationError(RequestContext context, string operationId, Exception error)
    {
        _writer.TryWrite(new ErrorInfo($"PlanOperationError (Operation: {operationId})", error.Message, error.StackTrace));
    }

    public override void SourceSchemaStoreError(OperationPlanContext context, ExecutionNode node, string schemaName, Exception error)
    {
        _writer.TryWrite(new ErrorInfo($"SourceSchemaStoreError (Schema: {schemaName}, Node: {node.Id})", error.Message, error.StackTrace));
    }

    public override void SourceSchemaResultError(OperationPlanContext context, ExecutionNode node, string schemaName, IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            _writer.TryWrite(new ErrorInfo($"SourceSchemaResultError (Schema: {schemaName}, Node: {node.Id})", error.Message, error.Exception?.StackTrace));
        }
    }

    public override void SourceSchemaTransportError(OperationPlanContext context, ExecutionNode node, string schemaName, Exception error)
    {
        _writer.TryWrite(new ErrorInfo($"SourceSchemaTransportError (Schema: {schemaName}, Node: {node.Id})", error.Message, error.StackTrace));
    }

    public record ErrorInfo(string Area, string Message, string? StackTrace = null);
}

public class MemoryPoolCollector : EventListener
{
    private readonly ChannelWriter<MemoryEventInfo> _writer;

    public MemoryPoolCollector()
    {
        var channel = Channel.CreateUnbounded<MemoryEventInfo>();
        _writer = channel.Writer;

        WriteEventsAsync(channel.Reader).FireAndForget();
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "HotChocolate-Fusion-FixedSizeArrayPool")
        {
            // Subscribe to all events at Verbose level to capture all buffer operations
            EnableEvents(eventSource, EventLevel.Verbose);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        var eventInfo = new MemoryEventInfo(
            Timestamp: DateTime.UtcNow,
            EventName: eventData.EventName ?? "Unknown",
            EventId: eventData.EventId,
            Level: eventData.Level.ToString(),
            Message: FormatMessage(eventData),
            Payload: FormatPayload(eventData)
        );

        _writer.TryWrite(eventInfo);
    }

    private string FormatMessage(EventWrittenEventArgs eventData)
    {
        if (eventData.Message == null || eventData.Payload == null)
        {
            return eventData.EventName ?? "Unknown";
        }

        try
        {
            return string.Format(eventData.Message, eventData.Payload.ToArray());
        }
        catch
        {
            return eventData.EventName ?? "Unknown";
        }
    }

    private string FormatPayload(EventWrittenEventArgs eventData)
    {
        if (eventData.PayloadNames == null || eventData.Payload == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        for (int i = 0; i < eventData.PayloadNames.Count && i < eventData.Payload.Count; i++)
        {
            parts.Add($"{eventData.PayloadNames[i]}={eventData.Payload[i]}");
        }

        return string.Join(", ", parts);
    }

    private async Task WriteEventsAsync(ChannelReader<MemoryEventInfo> reader)
    {
        await using var stream = File.Create("/Users/michael/local/hc-4/src/HotChocolate/Fusion-vnext/benchmarks/k6/eShop.Gateway/memory.log");
        await using var log = new StreamWriter(stream);

        await foreach (var ev in reader.ReadAllAsync())
        {
            await log.WriteLineAsync("-------");
            await log.WriteLineAsync($"[{ev.Timestamp:O}] {ev.EventName} (ID: {ev.EventId}, Level: {ev.Level})");
            await log.WriteLineAsync($"Message: {ev.Message}");

            if (!string.IsNullOrEmpty(ev.Payload))
            {
                await log.WriteLineAsync($"Payload: {ev.Payload}");
            }

            await log.FlushAsync();
        }
    }

    public record MemoryEventInfo(
        DateTime Timestamp,
        string EventName,
        int EventId,
        string Level,
        string Message,
        string Payload);
}

public class MetaDbCollector : EventListener
{
    private readonly ChannelWriter<MetaDbEventInfo> _writer;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<int, DbStats> _dbStats = new();
    private DateTime _lastEventTime = DateTime.UtcNow;
    private readonly Timer _checkTimer;
    private readonly object _checkLock = new();
    private bool _reportWritten;

    public MetaDbCollector()
    {
        var channel = Channel.CreateUnbounded<MetaDbEventInfo>();
        _writer = channel.Writer;

        WriteEventsAsync(channel.Reader).FireAndForget();

        // Check every 5 seconds if traffic has died down (5000ms initial delay, 5000ms period)
        _checkTimer = new Timer(CheckForLeaks, null, 5000, 5000);
    }

    private class DbStats
    {
        public int Created { get; init; }
        public int Disposed { get; init; }
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "HotChocolate-Fusion-MetaDb")
        {
            // Subscribe to all events at Verbose level to capture all MetaDb operations
            EnableEvents(eventSource, EventLevel.Verbose);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        var eventInfo = new MetaDbEventInfo(
            Timestamp: DateTime.UtcNow,
            EventName: eventData.EventName ?? "Unknown",
            EventId: eventData.EventId,
            Level: eventData.Level.ToString(),
            Message: FormatMessage(eventData),
            Payload: FormatPayload(eventData)
        );

        _writer.TryWrite(eventInfo);

        // Track creates and disposes per dbId
        if (eventData.EventId == 1 && eventData.Payload?.Count > 0) // MetaDbCreated
        {
            var dbId = Convert.ToInt32(eventData.Payload[0]);
            _dbStats.AddOrUpdate(dbId,
                _ => new DbStats { Created = 1, Disposed = 0 },
                (_, stats) => new DbStats { Created = stats.Created + 1, Disposed = stats.Disposed });
            _lastEventTime = DateTime.UtcNow;
            _reportWritten = false; // Reset flag when new activity happens
        }
        else if (eventData.EventId == 2 && eventData.Payload?.Count > 0) // MetaDbDisposed
        {
            var dbId = Convert.ToInt32(eventData.Payload[0]);
            _dbStats.AddOrUpdate(dbId,
                _ => new DbStats { Created = 0, Disposed = 1 },
                (_, stats) => new DbStats { Created = stats.Created, Disposed = stats.Disposed + 1 });
            _lastEventTime = DateTime.UtcNow;
            _reportWritten = false; // Reset flag when new activity happens
        }
    }

    private string FormatMessage(EventWrittenEventArgs eventData)
    {
        if (eventData.Message == null || eventData.Payload == null)
        {
            return eventData.EventName ?? "Unknown";
        }

        try
        {
            return string.Format(eventData.Message, eventData.Payload.ToArray());
        }
        catch
        {
            return eventData.EventName ?? "Unknown";
        }
    }

    private string FormatPayload(EventWrittenEventArgs eventData)
    {
        if (eventData.PayloadNames == null || eventData.Payload == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        for (int i = 0; i < eventData.PayloadNames.Count && i < eventData.Payload.Count; i++)
        {
            parts.Add($"{eventData.PayloadNames[i]}={eventData.Payload[i]}");
        }

        return string.Join(", ", parts);
    }

    private async Task WriteEventsAsync(ChannelReader<MetaDbEventInfo> reader)
    {
        await using var stream = File.Create("/Users/michael/local/hc-4/src/HotChocolate/Fusion-vnext/benchmarks/k6/eShop.Gateway/metadb.log");
        await using var log = new StreamWriter(stream);

        await foreach (var ev in reader.ReadAllAsync())
        {
            await log.WriteLineAsync("-------");
            await log.WriteLineAsync($"[{ev.Timestamp:O}] {ev.EventName} (ID: {ev.EventId}, Level: {ev.Level})");
            await log.WriteLineAsync($"Message: {ev.Message}");

            if (!string.IsNullOrEmpty(ev.Payload))
            {
                await log.WriteLineAsync($"Payload: {ev.Payload}");
            }

            await log.FlushAsync();
        }
    }

    private void CheckForLeaks(object? state)
    {
        lock (_checkLock)
        {
            // If no events for 10 seconds and we haven't written a report yet
            var timeSinceLastEvent = DateTime.UtcNow - _lastEventTime;
            if (timeSinceLastEvent.TotalSeconds >= 10 && !_reportWritten && _dbStats.Count > 0)
            {
                WriteLeakReport();
                _reportWritten = true;
            }
        }
    }

    private void WriteLeakReport()
    {
        try
        {
            using var stream = File.Create("/Users/michael/local/hc-4/src/HotChocolate/Fusion-vnext/benchmarks/k6/eShop.Gateway/metadb-check.log");
            using var log = new StreamWriter(stream);

            log.WriteLine("===========================================");
            log.WriteLine($"MetaDb Leak Detection Report");
            log.WriteLine($"Generated: {DateTime.UtcNow:O}");
            log.WriteLine("===========================================");
            log.WriteLine();

            var totalCreated = 0;
            var totalDisposed = 0;
            var leaksDetected = new List<(int dbId, int leaked)>();

            foreach (var kvp in _dbStats.OrderBy(x => x.Key))
            {
                var dbId = kvp.Key;
                var stats = kvp.Value;
                var leaked = stats.Created - stats.Disposed;

                totalCreated += stats.Created;
                totalDisposed += stats.Disposed;

                log.WriteLine($"DbId: {dbId}");
                log.WriteLine($"  Created:  {stats.Created}");
                log.WriteLine($"  Disposed: {stats.Disposed}");
                log.WriteLine($"  Balance:  {leaked} {(leaked > 0 ? "⚠️ LEAKED" : leaked < 0 ? "⚠️ OVER-DISPOSED" : "✓ OK")}");
                log.WriteLine();

                if (leaked != 0)
                {
                    leaksDetected.Add((dbId, leaked));
                }
            }

            log.WriteLine("===========================================");
            log.WriteLine("SUMMARY");
            log.WriteLine("===========================================");
            log.WriteLine($"Total MetaDb Created:  {totalCreated}");
            log.WriteLine($"Total MetaDb Disposed: {totalDisposed}");
            log.WriteLine($"Net Leaked:            {totalCreated - totalDisposed}");
            log.WriteLine();

            if (leaksDetected.Count > 0)
            {
                log.WriteLine($"⚠️  {leaksDetected.Count} DbId(s) with leaks detected:");
                foreach (var (dbId, leaked) in leaksDetected)
                {
                    log.WriteLine($"   DbId {dbId}: {Math.Abs(leaked)} {(leaked > 0 ? "leaked" : "over-disposed")}");
                }
            }
            else
            {
                log.WriteLine("✓ No leaks detected - all MetaDbs properly disposed!");
            }

            log.WriteLine("===========================================");
            log.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write leak report: {ex.Message}");
        }
    }

    public record MetaDbEventInfo(
        DateTime Timestamp,
        string EventName,
        int EventId,
        string Level,
        string Message,
        string Payload);
}
