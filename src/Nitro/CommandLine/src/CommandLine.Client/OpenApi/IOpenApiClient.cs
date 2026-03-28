using ChilliCream.Nitro.Client.Exceptions;
namespace ChilliCream.Nitro.Client.OpenApi;

/// <summary>
/// Provides remote OpenAPI collection operations used by OpenAPI commands.
/// </summary>
public interface IOpenApiClient
{
    /// <summary>
    /// Creates an OpenAPI collection.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection> CreateOpenApiCollectionAsync(
        string apiId,
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an OpenAPI collection.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById> DeleteOpenApiCollectionAsync(
        string openApiCollectionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists OpenAPI collections for an API.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ConnectionPage<IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node>> ListOpenApiCollectionsAsync(
        string apiId,
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Uploads an OpenAPI collection version.
    /// </summary>
    /// <returns>The uploaded version.</returns>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or payload errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection> UploadOpenApiCollectionVersionAsync(
        string openApiCollectionId,
        string tag,
        Stream collectionArchiveStream,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates an OpenAPI collection validation request.
    /// </summary>
    /// <returns>The validation request.</returns>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or payload errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection> StartOpenApiCollectionValidationAsync(
        string openApiCollectionId,
        string stageName,
        Stream collectionArchiveStream,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams OpenAPI collection validation updates for a request.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The subscription failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    IAsyncEnumerable<IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate> SubscribeToOpenApiCollectionValidationAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an OpenAPI collection publish request.
    /// </summary>
    /// <returns>The publish request.</returns>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or payload errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection> StartOpenApiCollectionPublishAsync(
        string openApiCollectionId,
        string stageName,
        string tag,
        bool force,
        bool waitForApproval,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams OpenAPI collection publish updates for a request.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The subscription failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    IAsyncEnumerable<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate> SubscribeToOpenApiCollectionPublishAsync(
        string requestId,
        CancellationToken cancellationToken = default);
}
