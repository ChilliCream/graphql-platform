using System.Text.Json;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

var targetPid = int.Parse(args[1]);
using var trace = new EventPipeEventSource(args[0]);
var events = new Dictionary<string, int>();
var chronology = new List<object>();
DateTime? firstEventUtc = null;
DateTime? lastEventUtc = null;
trace.Clr.All += entry =>
{
    if (entry.ProcessID != targetPid)
    {
        return;
    }

    firstEventUtc ??= entry.TimeStamp.ToUniversalTime();
    lastEventUtc = entry.TimeStamp.ToUniversalTime();
    var eventKey = $"{entry.ProviderName}/{entry.EventName}";
    events[eventKey] = events.GetValueOrDefault(eventKey) + 1;
    if (!entry.EventName.StartsWith("GC/", StringComparison.Ordinal)
        || entry.EventName.Contains("AllocationTick", StringComparison.Ordinal)
        || entry.EventName.Contains("Bulk", StringComparison.Ordinal))
    {
        return;
    }

    object gcStart = entry is GCStartTraceData gc
        ? new { gc.Count, gc.Depth, reason = gc.Reason.ToString(), type = gc.Type.ToString() }
        : null;
    chronology.Add(new
    {
        entry.ProcessID,
        entry.ThreadID,
        entry.EventName,
        utc = entry.TimeStamp.ToUniversalTime().ToString("O"),
        entry.TimeStampRelativeMSec,
        gcStart,
        payload = entry.PayloadNames.ToDictionary(name => name, name => entry.PayloadByName(name)?.ToString())
    });
};

string parseError = null;
try
{
    trace.Process();
}
catch (Exception error)
{
    parseError = error.ToString();
}

Console.WriteLine(JsonSerializer.Serialize(new
{
    targetPid,
    complete = parseError is null,
    parseError,
    firstEventUtc,
    lastEventUtc,
    traceStartUtc = trace.SessionStartTime.ToUniversalTime().ToString("O"),
    traceEndUtc = trace.SessionEndTime.ToUniversalTime().ToString("O"),
    reportedEventsLost = trace.EventsLost,
    events,
    chronology,
    limitation = "A truncated trace is only a readable event prefix; its final bounds and lost-event count cannot be certified. GC event chronology identifies observed triggers, not allocation performance. Payload values retain their textual representation. Correlate events with target identity and raw block timestamps."
}, new JsonSerializerOptions { WriteIndented = true }));
