namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record LogRow(
    string Id,
    double Start,
    string SeverityText,
    int SeverityNumber,
    string Body,
    string TraceId,
    string SpanId,
    string ServiceName);
