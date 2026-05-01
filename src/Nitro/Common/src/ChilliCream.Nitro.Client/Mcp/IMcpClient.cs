namespace ChilliCream.Nitro.Client.Mcp;

/// <summary>
/// Provides remote MCP feature collection operations used by MCP commands.
/// </summary>
public interface IMcpClient
{
    /// <summary>
    /// Creates an MCP feature collection.
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
    Task<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection> CreateMcpFeatureCollectionAsync(
        string apiId,
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an MCP feature collection.
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
    Task<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById> DeleteMcpFeatureCollectionAsync(
        string mcpFeatureCollectionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists MCP feature collections for an API.
    /// </summary>
    /// <returns>A page of results, or <c>null</c> if the API was not found.</returns>
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
    Task<ConnectionPage<IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node>?> ListMcpFeatureCollectionsAsync(
        string apiId,
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Uploads an MCP feature collection version.
    /// </summary>
    /// <returns>The uploaded version.</returns>
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
    Task<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection> UploadMcpFeatureCollectionVersionAsync(
        string mcpFeatureCollectionId,
        string tag,
        Stream collectionArchiveStream,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates an MCP feature collection validation request.
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
    Task<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection> StartMcpFeatureCollectionValidationAsync(
        string mcpFeatureCollectionId,
        string stageName,
        Stream collectionArchiveStream,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams MCP feature collection validation updates for a request.
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
    IAsyncEnumerable<IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate> SubscribeToMcpFeatureCollectionValidationAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an MCP feature collection publish request.
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
    Task<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection> StartMcpFeatureCollectionPublishAsync(
        string mcpFeatureCollectionId,
        string stageName,
        string tag,
        bool force,
        bool waitForApproval,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams MCP feature collection publish updates for a request.
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
    IAsyncEnumerable<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate> SubscribeToMcpFeatureCollectionPublishAsync(
        string requestId,
        CancellationToken cancellationToken = default);
}
