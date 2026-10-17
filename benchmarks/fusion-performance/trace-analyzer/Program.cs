using System.Text.Json;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

var path = TraceLog.CreateFromEventPipeDataFile(args[0]);
using var trace = new TraceLog(path);
var types = new Dictionary<string, (long Bytes, int Ticks)>();
var frames = new Dictionary<string, (long Bytes, int Ticks)>();
long total = 0;
var events = new Dictionary<string, int>();
var contentions = new Dictionary<string, (double Ms, int Count)>();
foreach (var entry in trace.Events)
{
    events[entry.EventName] = events.GetValueOrDefault(entry.EventName) + 1;
    if (!entry.EventName.Contains("Contention/Stop"))
    {
        continue;
    }
    var duration = Convert.ToDouble(entry.PayloadByName("DurationNs")) / 1_000_000;
    var stack = entry.CallStack();
    var names = new List<string>();
    while (stack is not null && names.Count < 32)
    {
        names.Add(stack.CodeAddress.FullMethodName);
        stack = stack.Caller;
    }
    var chain = string.Join(" <- ", names);
    var c = contentions.GetValueOrDefault(chain);
    contentions[chain] = (c.Ms + duration, c.Count + 1);
}
foreach (var traceEvent in trace.Events)
{
    if (traceEvent is not GCAllocationTickTraceData e)
    {
        continue;
    }
    var bytes = e.AllocationAmount64 > 0 ? e.AllocationAmount64 : e.AllocationAmount;
    total += bytes;
    var name = e.TypeName ?? "unknown";
    var t = types.GetValueOrDefault(name);
    types[name] = (t.Bytes + bytes, t.Ticks + 1);
    var stack = e.CallStack();
    var chain = new List<string>();
    while (stack is not null && chain.Count < 24)
    {
        chain.Add(stack.CodeAddress.FullMethodName);
        stack = stack.Caller;
    }
    var key = name + " :: " + string.Join(" <- ", chain);
    var f = frames.GetValueOrDefault(key);
    frames[key] = (f.Bytes + bytes, f.Ticks + 1);
}
Console.WriteLine(JsonSerializer.Serialize(new {
    sampledBytes = total,
    events,
    contentionCount = contentions.Sum(x => x.Value.Count),
    contentionMs = contentions.Sum(x => x.Value.Ms),
    contentions = contentions.OrderByDescending(x => x.Value.Ms).Select(x => new { chain = x.Key, ms = x.Value.Ms, count = x.Value.Count }),
    types = types.OrderByDescending(x => x.Value.Bytes).Take(100).Select(x => new {name=x.Key, bytes=x.Value.Bytes, ticks=x.Value.Ticks, percent=100.0*x.Value.Bytes/total}),
    frames = frames.OrderByDescending(x => x.Value.Bytes).Take(250).Select(x => new {name=x.Key, bytes=x.Value.Bytes, ticks=x.Value.Ticks, percent=100.0*x.Value.Bytes/total})
}, new JsonSerializerOptions { WriteIndented = true }));
