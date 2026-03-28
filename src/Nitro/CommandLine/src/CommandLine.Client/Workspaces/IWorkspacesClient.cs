using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.Client.Workspaces;

/// <summary>
/// Provides remote workspace operations used by workspace commands.
/// </summary>
public interface IWorkspacesClient
{
    /// <summary>
    /// Fetches a page of workspaces.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ConnectionPage<IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node>> ListWorkspacesAsync(
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fetches a page of workspaces for default-workspace selection.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>> SelectWorkspacesAsync(
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new workspace.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ICreateWorkspaceCommandMutation_CreateWorkspace> CreateWorkspaceAsync(
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads workspace details by identifier.
    /// </summary>
    /// <returns>The workspace details, or <c>null</c> if no workspace was found.</returns>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IShowWorkspaceCommandQuery_Node?> ShowWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken);
}
