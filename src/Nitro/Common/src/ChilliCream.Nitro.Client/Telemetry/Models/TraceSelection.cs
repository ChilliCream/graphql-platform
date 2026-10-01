namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record TraceSelection(
    TraceField Field,
    string? Name,
    string? Path,
    string? Type);
