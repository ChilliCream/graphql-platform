using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.Client.Environments;

/// <summary>
/// Provides remote environment operations used by environment commands.
/// </summary>
public interface IEnvironmentsClient
{
    /// <summary>
    /// Fetches a page of environments for a workspace.
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
    Task<ConnectionPage<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node>> ListEnvironmentsAsync(
        string workspaceId,
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new environment.
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
    Task<ICreateEnvironmentCommandMutation_PushWorkspaceChanges> CreateEnvironmentAsync(
        string workspaceId,
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads environment details by identifier.
    /// </summary>
    /// <returns>The environment details, or <c>null</c> if the environment was not found or the caller is not authorized.</returns>
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
    Task<IShowEnvironmentCommandQuery_Node?> GetEnvironmentAsync(
        string environmentId,
        CancellationToken cancellationToken);
}
