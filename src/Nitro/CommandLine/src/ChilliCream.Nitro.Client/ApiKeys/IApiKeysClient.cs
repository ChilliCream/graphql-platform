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
    Task<ConnectionPage<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node>> ListApiKeysAsync(
        string workspaceId,
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new API key.
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
    Task<ICreateApiKeyCommandMutation_CreateApiKey> CreateApiKeyAsync(
        string name,
        string workspaceId,
        string? apiId,
        string? stageConditionName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an API key.
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
    Task<IDeleteApiKeyCommandMutation_DeleteApiKey> DeleteApiKeyAsync(
        string keyId,
        CancellationToken cancellationToken);
}
