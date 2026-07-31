namespace ChilliCream.Nitro.Fusion.Transport;

internal interface IFusionDeploymentTransportFactory
{
    ValueTask<IFusionDeploymentTransport> OpenAsync(
        FusionTarget target,
        CancellationToken cancellationToken);
}

internal interface IFusionDeploymentTransport : IAsyncDisposable
{
    // The caller owns a non-null returned buffer and must clear it after use.
    Task<byte[]?> DownloadSourceSchemaAsync(
        string name,
        string version,
        CancellationToken cancellationToken);

    Task<FusionRemoteCommandResult> UploadSourceSchemaAsync(
        string version,
        string archivePath,
        CancellationToken cancellationToken);

    Task<FusionRemoteBeginResult> BeginPublishAsync(
        FusionPublicationRequest request,
        CancellationToken cancellationToken);

    IAsyncEnumerable<FusionRemoteEvent> WatchPublishAsync(
        string requestId,
        CancellationToken cancellationToken);

    Task<FusionRemoteCommandResult> ClaimPublishAsync(
        string requestId,
        CancellationToken cancellationToken);

    Task<FusionRemoteCommandResult> ReleasePublishAsync(
        string requestId,
        CancellationToken cancellationToken);

    Task<FusionRemoteCommandResult> ValidatePublishAsync(
        string requestId,
        ReadOnlyMemory<byte> archive,
        CancellationToken cancellationToken);

    Task<FusionRemoteCommandResult> CommitPublishAsync(
        string requestId,
        ReadOnlyMemory<byte> archive,
        CancellationToken cancellationToken);
}

internal sealed record FusionRemoteCommandResult(
    bool Succeeded,
    bool IsDuplicate,
    IReadOnlyList<string> Errors)
{
    public static FusionRemoteCommandResult Success { get; } =
        new(true, false, []);
}

internal sealed record FusionRemoteBeginResult(
    string? RequestId,
    IReadOnlyList<string> Errors);

internal sealed record FusionRemoteEvent(
    FusionRemoteEventKind Kind,
    IReadOnlyList<string> Errors);

internal enum FusionRemoteEventKind
{
    Ready,
    Queued,
    ValidationSucceeded,
    ValidationFailed,
    PublishingSucceeded,
    PublishingFailed,
    InProgress,
    WaitingForApproval,
    Approved,
    Unknown
}
