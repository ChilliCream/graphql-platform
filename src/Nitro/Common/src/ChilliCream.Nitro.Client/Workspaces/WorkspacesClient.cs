namespace ChilliCream.Nitro.Client.Workspaces;

internal sealed class WorkspacesClient(IApiClient apiClient) : IWorkspacesClient
{
    public async Task<ConnectionPage<IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node>> ListWorkspacesAsync(
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListWorkspaceCommandQuery.ExecuteAsync(after, first, cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        var connection = data.Me?.Workspaces;
        var items = connection?.Edges?.Select(static t => t.Node).ToArray() ?? [];

        return new ConnectionPage<IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }

    public async Task<ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>> SelectWorkspacesAsync(
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.SetDefaultWorkspaceCommand_SelectWorkspace_Query.ExecuteAsync(
            after,
            first,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        var connection = data.Me?.Workspaces;
        var items = connection?.Edges?.Select(static t => t.Node).ToArray() ?? [];

        return new ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }

    public async Task<ICreateWorkspaceCommandMutation_CreateWorkspace> CreateWorkspaceAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var input = new CreateWorkspaceInput { Name = name };
        var result = await apiClient.CreateWorkspaceCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).CreateWorkspace;
    }

    public async Task<IShowWorkspaceCommandQuery_Node?> GetWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ShowWorkspaceCommandQuery.ExecuteAsync(workspaceId, cancellationToken);

        return OperationResultHelper.EnsureData(result).Node;
    }
}
