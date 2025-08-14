using ChilliCream.Nitro.CLI.Client;

namespace ChilliCream.Nitro.CLI;

public sealed class SelectApiPrompt(IApiClient client, string workspaceId)
{
    private string _title = "Select the api you want to use.";

    public SelectApiPrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<ISelectApiPrompt_Api?> RenderAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        var paginationContainer = PaginationContainer.Create(
            (after, first, ct)
                => client.SelectApiPromptQuery.ExecuteAsync(workspaceId, after, first, ct),
            p => p.WorkspaceById?.Apis?.PageInfo,
            p => p.WorkspaceById?.Apis?.Edges);

        var selectedEdge = await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(_title)
            .UseConverter(x => x.Node.Name)
            .RenderAsync(console, cancellationToken);

        return selectedEdge?.Node;
    }

    public static SelectApiPrompt New(IApiClient client, string workspaceId)
        => new(client, workspaceId);
}
