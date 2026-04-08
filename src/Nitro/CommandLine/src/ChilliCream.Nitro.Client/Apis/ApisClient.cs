using GeneratedApiKind = ChilliCream.Nitro.Client.ApiKind;

namespace ChilliCream.Nitro.Client.Apis;

internal sealed class ApisClient(IApiClient apiClient) : IApisClient
{
    public async Task<ConnectionPage<IListApiCommandQuery_WorkspaceById_Apis_Edges_Node>> ListApisAsync(
        string workspaceId,
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListApiCommandQuery.ExecuteAsync(
            workspaceId,
            after,
            first,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        var connection = data.WorkspaceById?.Apis;
        var items = connection?.Edges?.Select(static t => t.Node).ToArray() ?? [];

        return new ConnectionPage<IListApiCommandQuery_WorkspaceById_Apis_Edges_Node>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }

    public async Task<ConnectionPage<ISelectApiPromptQuery_WorkspaceById_Apis_Edges_Node>> SelectApisAsync(
        string workspaceId,
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.SelectApiPromptQuery.ExecuteAsync(
            workspaceId,
            after,
            first,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        var connection = data.WorkspaceById?.Apis;
        var items = connection?.Edges?.Select(static t => t.Node).ToArray() ?? [];

        return new ConnectionPage<ISelectApiPromptQuery_WorkspaceById_Apis_Edges_Node>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }

    public async Task<ICreateApiCommandMutation_PushWorkspaceChanges> CreateApiAsync(
        string workspaceId,
        IReadOnlyList<string> path,
        string name,
        ApiKind? kind,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.CreateApiCommandMutation.ExecuteAsync(
            workspaceId,
            path,
            name,
            MapApiKind(kind),
            cancellationToken);

        return OperationResultHelper.EnsureData(result).PushWorkspaceChanges;
    }

    public async Task<IDeleteApiCommandQuery_Node?> GetApiForDeleteAsync(
        string apiId,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.DeleteApiCommandQuery.ExecuteAsync(apiId, cancellationToken);

        return OperationResultHelper.EnsureData(result).Node;
    }

    public async Task<IDeleteApiCommandMutation_DeleteApiById> DeleteApiAsync(
        string apiId,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.DeleteApiCommandMutation.ExecuteAsync(apiId, cancellationToken);

        return OperationResultHelper.EnsureData(result).DeleteApiById;
    }

    public async Task<IShowApiCommandQuery_Node?> GetApiAsync(
        string apiId,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ShowApiCommandQuery.ExecuteAsync(apiId, cancellationToken);

        return OperationResultHelper.EnsureData(result).Node;
    }

    public async Task<ISetApiSettingsCommandMutation_UpdateApiSettings> UpdateApiSettingsAsync(
        string apiId,
        bool treatDangerousAsBreaking,
        bool allowBreakingSchemaChanges,
        CancellationToken cancellationToken)
    {
        var input = new UpdateApiSettingsInput
        {
            ApiId = apiId,
            Settings = new PartialApiSettingsInput
            {
                SchemaRegistry = new PartialSchemaRegistrySettingsInput
                {
                    TreatDangerousAsBreaking = treatDangerousAsBreaking,
                    AllowBreakingSchemaChanges = allowBreakingSchemaChanges
                }
            }
        };

        var result = await apiClient.SetApiSettingsCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).UpdateApiSettings;
    }

    private static GeneratedApiKind? MapApiKind(ApiKind? kind)
        => kind switch
        {
            ApiKind.Collection => GeneratedApiKind.Collection,
            ApiKind.Service => GeneratedApiKind.Service,
            ApiKind.Gateway => GeneratedApiKind.Gateway,
            null => null,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported API kind.")
        };
}
