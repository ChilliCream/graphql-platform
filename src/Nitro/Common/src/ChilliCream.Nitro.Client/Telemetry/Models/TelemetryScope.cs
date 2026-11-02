namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record TelemetryScope(
    string? Name,
    string? SchemaUrl,
    string? Version,
    IReadOnlyList<TypedTelemetryAttribute> Attributes);
