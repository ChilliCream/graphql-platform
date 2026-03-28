using ChilliCream.Nitro.Client.Exceptions;

namespace ChilliCream.Nitro.Client.FusionConfiguration;

/// <summary>
/// Provides all remote operations required by Fusion commands.
/// </summary>
public interface IFusionConfigurationClient
{
    /// <summary>
    /// Requests a deployment slot for publishing a Fusion configuration.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The deployment slot request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    Task<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish> RequestDeploymentSlotAsync(
        string apiId,
        string stageName,
        string tag,
        string? subgraphId,
        string? subgraphName,
        SourceSchemaVersion[]? sourceSchemaVersions,
        bool waitForApproval,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams updates for a Fusion configuration publishing request.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The subscription failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    IAsyncEnumerable<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged> SubscribeToFusionConfigurationPublishingTaskChangedAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts composition for an existing deployment slot.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The operation failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    Task<IStartFusionConfigurationPublish_StartFusionConfigurationComposition> ClaimDeploymentSlotAsync(
        string requestId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cancels composition for an existing deployment slot.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The operation failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    Task<ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition> ReleaseDeploymentSlotAsync(
        string requestId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Commits a Fusion archive to an existing deployment slot.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The operation failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    Task<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish> CommitFusionArchiveAsync(
        string requestId,
        Stream stream,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates a Fusion configuration archive for an existing deployment slot.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The operation failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    Task<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition> ValidateFusionConfigurationPublishAsync(
        string requestId,
        Stream stream,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a schema validation request and returns its request identifier.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The operation failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    Task<IValidateSchemaVersion_ValidateSchema> ValidateSchemaVersionAsync(
        string apiId,
        string stageName,
        Stream schema,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams updates for a schema validation request.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The subscription failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    IAsyncEnumerable<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate> SubscribeToSchemaVersionValidationUpdatedAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a source schema archive.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The operation failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    Task<IUploadFusionSubgraph_UploadFusionSubgraph> UploadFusionSubgraphAsync(
        string apiId,
        string tag,
        Stream archive,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the latest Fusion v2 archive (.far) for the specified stage.
    /// </summary>
    /// <returns>The archive stream, or <c>null</c> if no archive exists.</returns>
    /// <remarks>The caller owns and must dispose the returned stream.</remarks>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="NitroClientException">
    /// The download failed due to other transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    Task<Stream?> DownloadLatestFusionArchiveAsync(
        string apiId,
        string stageName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the latest legacy Fusion v1 archive (.fgp) for the specified stage.
    /// </summary>
    /// <returns>The archive stream, or <c>null</c> if no archive exists.</returns>
    /// <remarks>The caller owns and must dispose the returned stream.</remarks>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="NitroClientException">
    /// The download failed due to other transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    Task<Stream?> DownloadLatestLegacyFusionArchiveAsync(
        string apiId,
        string stageName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a source schema archive stream for a specific source schema version.
    /// </summary>
    /// <remarks>The caller owns and must dispose the returned stream.</remarks>
    /// <exception cref="NitroClientNotFoundException">
    /// The requested source schema version does not exist.
    /// </exception>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="NitroClientException">
    /// The download failed due to other transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    Task<Stream> DownloadSourceSchemaArchiveAsync(
        string apiId,
        string sourceSchemaName,
        string sourceSchemaVersion,
        CancellationToken cancellationToken);
}
