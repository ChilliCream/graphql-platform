namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record HttpTraceSpanData(
    string? Flavor,
    string? Method,
    string? Scheme,
    int? StatusCode,
    string? Url,
    string? UserAgent) : TraceSpanData;
