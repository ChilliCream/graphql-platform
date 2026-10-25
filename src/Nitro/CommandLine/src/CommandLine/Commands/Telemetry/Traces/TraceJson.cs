using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces;

internal sealed record TraceJson(
    string TraceId,
    int? SpanCount,
    bool SpansTruncated,
    double TotalDurationMs,
    IReadOnlyList<TraceJsonSpan> Spans)
{
    public static TraceJson From(string traceId, Trace trace)
        => new(
            traceId,
            trace.SpanCount,
            trace.SpansTruncated,
            trace.TotalDuration,
            trace.Spans.Select(TraceJsonSpan.From).ToArray());
}
