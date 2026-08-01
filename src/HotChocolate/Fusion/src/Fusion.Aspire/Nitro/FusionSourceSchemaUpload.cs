namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Describes an immutable Fusion source schema archive to reconcile.
/// </summary>
internal sealed record FusionSourceSchemaUpload(
    string Name,
    string Version,
    string ArchivePath,
    string Sha256);
