using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;

public sealed class SelectMcpFeatureCollectionPrompt(IApiClient client, string apiId)
{
    private string _title = "Select an MCP Feature Collection from the list below.";

    public SelectMcpFeatureCollectionPrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<ISelectMcpFeatureCollectionPrompt_McpFeatureCollection?> RenderAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        var paginationContainer = PaginationContainer.Create(
            (after, first, ct)
                => client.SelectMcpFeatureCollectionPromptQuery.ExecuteAsync(apiId, after, first, ct),
            p => p.ApiById?.McpFeatureCollections?.PageInfo,
            p => p.ApiById?.McpFeatureCollections?.Edges);

        var selectedEdge = await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(_title)
            .UseConverter(x => x.Node.Name)
            .RenderAsync(console, cancellationToken);

        return selectedEdge?.Node;
    }

    public static SelectMcpFeatureCollectionPrompt New(IApiClient client, string apiId)
        => new(client, apiId);
}
