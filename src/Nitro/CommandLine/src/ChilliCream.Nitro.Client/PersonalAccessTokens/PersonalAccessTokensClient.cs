namespace ChilliCream.Nitro.Client.PersonalAccessTokens;

internal sealed class PersonalAccessTokensClient(IApiClient apiClient) : IPersonalAccessTokensClient
{
    public async Task<ConnectionPage<IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node>> ListPersonalAccessTokensAsync(
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListPersonalAccessTokenCommandQuery.ExecuteAsync(
            after,
            first,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        var connection = data.Me?.PersonalAccessTokens;
        var items = connection?.Edges?.Select(static t => t.Node).ToArray() ?? [];

        return new ConnectionPage<IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }

    public async Task<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken> CreatePersonalAccessTokenAsync(
        string description,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var input = new CreatePersonalAccessTokenInput
        {
            Description = description,
            ExpiresAt = expiresAt
        };

        var result = await apiClient.CreatePersonalAccessTokenCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).CreatePersonalAccessToken;
    }

    public async Task<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken> RevokePersonalAccessTokenAsync(
        string patId,
        CancellationToken cancellationToken)
    {
        var input = new RevokePersonalAccessTokenInput { Id = patId };
        var result = await apiClient.RevokePersonalAccessTokenCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).RevokePersonalAccessToken;
    }
}
