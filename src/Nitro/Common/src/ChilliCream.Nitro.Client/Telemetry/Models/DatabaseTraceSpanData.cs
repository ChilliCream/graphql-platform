namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record DatabaseTraceSpanData(
    string? ConnectionString,
    string? Instance,
    string? Name,
    string? Operation,
    string? Statement,
    string? System,
    string? Url,
    string? User) : TraceSpanData;
