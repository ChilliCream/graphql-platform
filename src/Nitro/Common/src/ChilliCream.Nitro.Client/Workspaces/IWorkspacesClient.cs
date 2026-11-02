namespace ChilliCream.Nitro.Client.Workspaces;

/// <summary>
/// Provides remote workspace operations used by workspace commands.
/// </summary>
public interface IWorkspacesClient
{
    /// <summary>
    /// Fetches a page of workspaces.
    /// </summary>
    /// <returns>A page of results. The page may be empty if no items exist or the caller is not authorized.</returns>
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
    Task<ConnectionPage<IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node>> ListWorkspacesAsync(
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fetches a page of workspaces for default-workspace selection.
    /// </summary>
    /// <returns>A page of results. The page may be empty if no items exist or the caller is not authorized.</returns>
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
    Task<ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>> SelectWorkspacesAsync(
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new workspace.
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
    Task<ICreateWorkspaceCommandMutation_CreateWorkspace> CreateWorkspaceAsync(
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads workspace details by identifier.
    /// </summary>
    /// <returns>The workspace details, or <c>null</c> if the workspace was not found or the caller is not authorized.</returns>
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
    Task<IShowWorkspaceCommandQuery_Node?> GetWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken);
}
