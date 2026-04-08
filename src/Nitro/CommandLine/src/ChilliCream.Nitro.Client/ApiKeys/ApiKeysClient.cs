using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.Client.ApiKeys;

internal sealed class ApiKeysClient(IApiClient apiClient) : IApiKeysClient
{
    public async Task<ConnectionPage<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node>> ListApiKeysAsync(
        string workspaceId,
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListApiKeyCommandQuery.ExecuteAsync(
            workspaceId,
            after,
            first,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        var connection = data.WorkspaceById?.ApiKeys;
        var items = connection?.Edges?.Select(static t => t.Node).ToArray() ?? [];

        return new ConnectionPage<IListApiKeyCommandQuery_WorkspaceById_ApiKeys_Edges_Node>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }

    public async Task<ICreateApiKeyCommandMutation_CreateApiKey> CreateApiKeyAsync(
        string name,
        string workspaceId,
        string? apiId,
        string? stageConditionName,
        CancellationToken cancellationToken)
    {
        RoleAssigmentConditionInput? condition = null;
        if (!string.IsNullOrWhiteSpace(stageConditionName))
        {
            condition = new RoleAssigmentConditionInput
            {
                StageAuthorizationCondition = new RoleAssignmentStageAuthorizationConditionInput
                {
                    Name = stageConditionName
                }
            };
        }

        var input = new CreateApiKeyInput
        {
            Name = name,
            PermissionScope = apiId is not null
                ? new ApiKeyPermissionScopeInput { ApiId = apiId }
                : new ApiKeyPermissionScopeInput { WorkspaceId = workspaceId },
            WorkspaceId = workspaceId,
            RoleAssigmentCondition = condition
        };

        var result = await apiClient.CreateApiKeyCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).CreateApiKey;
    }

    public async Task<IDeleteApiKeyCommandMutation_DeleteApiKey> DeleteApiKeyAsync(
        string keyId,
        CancellationToken cancellationToken)
    {
        var input = new DeleteApiKeyInput { ApiKeyId = keyId };
        var result = await apiClient.DeleteApiKeyCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).DeleteApiKey;
    }
}
