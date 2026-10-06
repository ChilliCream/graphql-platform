namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record Log(
    string Id,
    double Start,
    string SeverityText,
    int SeverityNumber,
    string Body,
    string TraceId,
    string SpanId,
    LogBodyDetail BodyDetail,
    IReadOnlyList<TypedTelemetryAttribute> LogAttributes,
    IReadOnlyList<TelemetryAttribute> ResourceAttributes,
    TelemetryScope? Scope);
