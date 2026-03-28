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
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ConnectionPage<IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node>> ListPersonalAccessTokensAsync(
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a personal access token.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken> CreatePersonalAccessTokenAsync(
        string description,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a personal access token.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken> RevokePersonalAccessTokenAsync(
        string patId,
        CancellationToken cancellationToken);
}
