namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record TypedTelemetryAttribute(string Key, bool? Boolean, double? Float, long? Long, string? String);
