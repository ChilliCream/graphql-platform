namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Selects one immutable source schema version for a Fusion publication.
/// </summary>
internal sealed record FusionSourceSchemaVersion(string Name, string Version);
