namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record TraceEvent(string Name, double Start, IReadOnlyList<TelemetryAttribute> Attributes);
