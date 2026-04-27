using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;

internal sealed class SelectMcpFeatureCollectionPrompt(IMcpClient client, string apiId)
{
    private string _title = "Select an MCP Feature Collection from the list below.";

    public SelectMcpFeatureCollectionPrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node?> RenderAsync(
        INitroConsole console,
        CancellationToken cancellationToken)
    {
        var paginationContainer = PaginationContainer.CreateConnectionData(
            async (after, first, ct) => await client.ListMcpFeatureCollectionsAsync(apiId, after, first, ct)
                ?? throw new ExitException("The API was not found."));

        return await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(_title)
            .UseConverter(x => x.Name)
            .RenderAsync(console, cancellationToken);
    }

    public static SelectMcpFeatureCollectionPrompt New(IMcpClient client, string apiId)
        => new(client, apiId);
}
