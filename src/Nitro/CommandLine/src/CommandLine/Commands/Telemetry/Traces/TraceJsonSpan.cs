using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces;

internal sealed record TraceJsonSpan(
    string SpanId,
    string ParentSpanId,
    string SpanName,
    string SpanKind,
    double DurationMs,
    double Start,
    string StatusCode,
    string StatusMessage,
    IReadOnlyList<TelemetryAttribute> ResourceAttributes,
    IReadOnlyList<TelemetryAttribute> SpanAttributes,
    IReadOnlyList<TraceEvent> Events,
    TraceJsonData? Data)
{
    public static TraceJsonSpan From(TraceSpan span)
        => new(
            span.SpanId,
            span.ParentSpanId,
            span.SpanName,
            span.SpanKind,
            span.DurationMs,
            span.Start,
            span.StatusCode,
            span.StatusMessage,
            span.ResourceAttributes,
            span.SpanAttributes,
            span.Events,
            TraceJsonData.From(span.Data));
}
