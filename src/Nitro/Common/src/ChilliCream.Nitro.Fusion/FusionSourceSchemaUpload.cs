namespace ChilliCream.Nitro.Fusion;

/// <summary>
/// Describes an immutable Fusion source schema archive to reconcile.
/// </summary>
public sealed record FusionSourceSchemaUpload(
    string Name,
    string Version,
    string ArchivePath,
    string Sha256);
