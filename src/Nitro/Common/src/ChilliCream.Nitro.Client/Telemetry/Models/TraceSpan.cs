namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record TraceSpan(
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
    TraceSpanData? Data);
