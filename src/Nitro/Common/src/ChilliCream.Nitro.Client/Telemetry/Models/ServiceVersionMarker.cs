namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record ServiceVersionMarker(DateTimeOffset FirstSeenAt, string Version);
