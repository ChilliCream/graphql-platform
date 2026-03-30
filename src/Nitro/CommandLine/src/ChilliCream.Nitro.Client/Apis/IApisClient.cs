using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.Client.Apis;

/// <summary>
/// Provides remote API-management operations used by API commands.
/// </summary>
public interface IApisClient
{
    /// <summary>
    /// Fetches a page of APIs in a workspace.
    /// </summary>
    /// <returns>A page of results. The page may be empty if no items exist or the caller is not authorized.</returns>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ConnectionPage<IListApiCommandQuery_WorkspaceById_Apis_Edges_Node>> ListApisAsync(
        string workspaceId,
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fetches a page of APIs for interactive selection.
    /// </summary>
    /// <returns>A page of results. The page may be empty if no items exist or the caller is not authorized.</returns>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ConnectionPage<ISelectApiPromptQuery_WorkspaceById_Apis_Edges_Node>> SelectApisAsync(
        string workspaceId,
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new API.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ICreateApiCommandMutation_PushWorkspaceChanges> CreateApiAsync(
        string workspaceId,
        IReadOnlyList<string> path,
        string name,
        ApiKind? kind,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads API information used for delete confirmation.
    /// </summary>
    /// <returns>The API information, or <c>null</c> if the API was not found or the caller is not authorized.</returns>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IDeleteApiCommandQuery_Node?> GetApiForDeleteAsync(
        string apiId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an API.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IDeleteApiCommandMutation_DeleteApiById> DeleteApiAsync(
        string apiId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads API details by identifier.
    /// </summary>
    /// <returns>The API details, or <c>null</c> if the API was not found or the caller is not authorized.</returns>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IShowApiCommandQuery_Node?> GetApiAsync(
        string apiId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates API schema-registry settings.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="ChilliCream.Nitro.Client.NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ISetApiSettingsCommandMutation_UpdateApiSettings> UpdateApiSettingsAsync(
        string apiId,
        bool treatDangerousAsBreaking,
        bool allowBreakingSchemaChanges,
        CancellationToken cancellationToken);
}
