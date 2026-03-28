using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;

public sealed class SelectMcpFeatureCollectionPrompt(IMcpClient client, string apiId)
{
    private string _title = "Select an MCP Feature Collection from the list below.";

    public SelectMcpFeatureCollectionPrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node?> RenderAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        var paginationContainer = PaginationContainer.CreateConnectionData(
            (after, first, ct) => client.ListMcpFeatureCollectionsAsync(apiId, after, first, ct));

        return await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(_title)
            .UseConverter(x => x.Name)
            .RenderAsync(console, cancellationToken);
    }

    public static SelectMcpFeatureCollectionPrompt New(IMcpClient client, string apiId)
        => new(client, apiId);
}
