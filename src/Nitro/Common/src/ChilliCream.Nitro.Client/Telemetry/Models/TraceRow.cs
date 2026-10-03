namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record TraceRow(
    string TraceId,
    string SpanId,
    string Seeker,
    string SpanName,
    string SpanKind,
    double DurationMs,
    double Start,
    string StatusCode,
    string ServiceName);
