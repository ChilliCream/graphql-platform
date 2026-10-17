using System.Text.Json;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

var targetPid = int.Parse(args[1]);
var converted = TraceLog.CreateFromEventPipeDataFile(args[0]);
using var trace = new TraceLog(converted);
var httpEvents = new List<object>();
var allocationEvents = new List<object>();
var events = new Dictionary<string, int>();
var frames = new Dictionary<string, (long Bytes, int Ticks)>();
long sampledBytes = 0;
foreach (var entry in trace.Events)
{
    if (entry.ProcessID != targetPid)
    {
        continue;
    }

    var eventKey = $"{entry.ProviderName}/{entry.EventName}";
    events[eventKey] = events.GetValueOrDefault(eventKey) + 1;
    if (entry.ProviderName == "System.Net.Http")
    {
        httpEvents.Add(new
        {
            entry.ProcessID,
            entry.EventName,
            utc = entry.TimeStamp.ToUniversalTime().ToString("O"),
            entry.ActivityID,
            entry.RelatedActivityID,
            payload = entry.PayloadNames.ToDictionary(name => name, entry.PayloadByName)
        });
    }

    if (entry is not GCAllocationTickTraceData allocation)
    {
        continue;
    }

    var bytes = allocation.AllocationAmount64 > 0
        ? allocation.AllocationAmount64
        : allocation.AllocationAmount;
    sampledBytes += bytes;
    var stack = allocation.CallStack();
    var names = new List<string>();
    while (stack is not null && names.Count < 64)
    {
        names.Add(stack.CodeAddress.FullMethodName);
        stack = stack.Caller;
    }

    var chain = $"{allocation.TypeName} :: {string.Join(" <- ", names)}";
    var old = frames.GetValueOrDefault(chain);
    frames[chain] = (old.Bytes + bytes, old.Ticks + 1);
    allocationEvents.Add(new
    {
        allocation.ProcessID,
        allocation.ThreadID,
        utc = allocation.TimeStamp.ToUniversalTime().ToString("O"),
        allocation.TypeName,
        bytes,
        usedLegacyAmountFallback = allocation.AllocationAmount64 <= 0,
        chain
    });
}

Console.WriteLine(JsonSerializer.Serialize(new
{
    targetPid,
    traceStartUtc = trace.SessionStartTime.ToUniversalTime().ToString("O"),
    traceEndUtc = trace.SessionEndTime.ToUniversalTime().ToString("O"),
    reportedEventsLost = trace.EventsLost,
    events,
    sampledBytes,
    frames = frames.OrderByDescending(pair => pair.Value.Bytes).Select(pair => new
    {
        chain = pair.Key,
        bytes = pair.Value.Bytes,
        ticks = pair.Value.Ticks
    }),
    allocationEvents,
    httpEvents,
    limitation = "Sampled allocation amounts locate callers. They are not exact per-type allocated bytes or invocation counts. Validate HTTP event loss, destination, activity pairing and traffic containment before frequency claims."
}, new JsonSerializerOptions { WriteIndented = true }));
