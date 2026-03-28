using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.Client.ApiKeys;

/// <summary>
/// Provides remote API key operations used by API key commands.
/// </summary>
public interface IApiKeysClient
{
    /// <summary>
    /// Fetches a page of API keys for a workspace.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ConnectionPage<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node>> ListApiKeysAsync(
        string workspaceId,
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new API key.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ICreateApiKeyCommandMutation_CreateApiKey> CreateApiKeyAsync(
        string name,
        string workspaceId,
        string? apiId,
        string? stageConditionName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an API key.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IDeleteApiKeyCommandMutation_DeleteApiKey> DeleteApiKeyAsync(
        string keyId,
        CancellationToken cancellationToken);
}
