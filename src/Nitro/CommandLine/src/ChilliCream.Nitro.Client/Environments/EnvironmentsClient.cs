using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.Client.Environments;

internal sealed class EnvironmentsClient(IApiClient apiClient) : IEnvironmentsClient
{
    public async Task<ConnectionPage<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node>> ListEnvironmentsAsync(
        string workspaceId,
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListEnvironmentCommandQuery.ExecuteAsync(
            workspaceId,
            after,
            first,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        var connection = data.WorkspaceById?.Environments;
        var items = connection?.Edges?.Select(static t => t.Node).ToArray() ?? [];

        return new ConnectionPage<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }

    public async Task<ICreateEnvironmentCommandMutation_PushWorkspaceChanges> CreateEnvironmentAsync(
        string workspaceId,
        string name,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.CreateEnvironmentCommandMutation.ExecuteAsync(
            workspaceId,
            name,
            cancellationToken);

        return OperationResultHelper.EnsureData(result).PushWorkspaceChanges;
    }

    public async Task<IShowEnvironmentCommandQuery_Node?> GetEnvironmentAsync(
        string environmentId,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ShowEnvironmentCommandQuery.ExecuteAsync(environmentId, cancellationToken);

        return OperationResultHelper.EnsureData(result).Node;
    }
}
