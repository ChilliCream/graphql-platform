using ChilliCream.Nitro.Client;
namespace ChilliCream.Nitro.Client.PersonalAccessTokens;

/// <summary>
/// Provides remote personal-access-token operations used by PAT commands.
/// </summary>
public interface IPersonalAccessTokensClient
{
    /// <summary>
    /// Fetches a page of personal access tokens.
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
    Task<ConnectionPage<IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node>> ListPersonalAccessTokensAsync(
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a personal access token.
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
    Task<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken> CreatePersonalAccessTokenAsync(
        string description,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a personal access token.
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
    Task<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken> RevokePersonalAccessTokenAsync(
        string patId,
        CancellationToken cancellationToken);
}
