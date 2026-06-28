namespace ChilliCream.Nitro.Client.Schemas;

/// <summary>
/// Provides remote schema operations used by schema commands.
/// </summary>
public interface ISchemasClient
{
    /// <summary>
    /// Uploads a schema version for an API and tag.
    /// </summary>
    /// <returns>The uploaded schema version.</returns>
    /// <exception cref="NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IUploadSchema_UploadSchema> UploadSchemaAsync(
        string apiId,
        string tag,
        Stream schemaStream,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a schema validation request.
    /// </summary>
    /// <returns>The validation request.</returns>
    /// <exception cref="NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IValidateSchemaVersion_ValidateSchema> StartSchemaValidationAsync(
        string apiId,
        string stageName,
        Stream schemaStream,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams schema validation updates for a request.
    /// </summary>
    /// <exception cref="NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    IAsyncEnumerable<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate> SubscribeToSchemaValidationAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a schema publish request.
    /// </summary>
    /// <returns>The publish request.</returns>
    /// <exception cref="NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IPublishSchemaVersion_PublishSchema> StartSchemaPublishAsync(
        string apiId,
        string stageName,
        string tag,
        bool force,
        bool waitForApproval,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams schema publish updates for a request.
    /// </summary>
    /// <exception cref="NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    IAsyncEnumerable<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate> SubscribeToSchemaPublishAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the latest published schema for the given API and stage.
    /// </summary>
    /// <returns>The schema stream, or <c>null</c> if no schema was found.</returns>
    /// <remarks>The caller owns and must dispose the returned stream.</remarks>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<Stream?> DownloadLatestSchemaAsync(
        string apiId,
        string stageName,
        CancellationToken cancellationToken);
}
