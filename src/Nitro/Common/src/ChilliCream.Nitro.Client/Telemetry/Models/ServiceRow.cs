namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record ServiceRow(
    string Name,
    IReadOnlyList<string> EnvironmentNames,
    IReadOnlyList<ServiceVersionMarker> VersionMarkers);
