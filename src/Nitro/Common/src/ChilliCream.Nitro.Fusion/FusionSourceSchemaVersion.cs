namespace ChilliCream.Nitro.Fusion;

/// <summary>
/// Selects one immutable source schema version for a Fusion publication.
/// </summary>
public sealed record FusionSourceSchemaVersion(string Name, string Version);
