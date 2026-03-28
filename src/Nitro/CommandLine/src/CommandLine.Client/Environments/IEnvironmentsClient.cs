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
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
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
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ICreateEnvironmentCommandMutation_PushWorkspaceChanges> CreateEnvironmentAsync(
        string workspaceId,
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads environment details by identifier.
    /// </summary>
    /// <returns>The environment details, or <c>null</c> if no environment was found.</returns>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IShowEnvironmentCommandQuery_Node?> ShowEnvironmentAsync(
        string environmentId,
        CancellationToken cancellationToken);
}
