namespace ChilliCream.Nitro.Fusion;

/// <summary>
/// Reconciles Fusion source schema versions and publishes Fusion configurations.
/// </summary>
public interface IFusionDeploymentWorkflow
{
    /// <summary>
    /// Downloads an exact immutable source schema version and computes its canonical content
    /// identity. The caller owns a non-null result and must dispose it after use.
    /// </summary>
    Task<FusionSourceSchemaDownload?> DownloadSourceSchemaAsync(
        FusionTarget target,
        FusionSourceSchemaVersion source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ensures that an immutable source schema version exists with the expected content.
    /// </summary>
    Task ReconcileSourceSchemaAsync(
        FusionTarget target,
        FusionSourceSchemaUpload source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Publishes a locally composed Fusion archive and waits for a verified terminal result.
    /// </summary>
    Task PublishAsync(
        FusionPublicationRequest request,
        ReadOnlyMemory<byte> fusionArchive,
        CancellationToken cancellationToken);
}
