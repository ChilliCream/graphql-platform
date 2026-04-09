using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;

internal sealed class SelectOpenApiCollectionPrompt(IOpenApiClient client, string apiId)
{
    private string _title = "Select an OpenAPI collection from the list below.";

    public SelectOpenApiCollectionPrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node?> RenderAsync(
        INitroConsole console,
        CancellationToken cancellationToken)
    {
        var paginationContainer = PaginationContainer.CreateConnectionData(
            async (after, first, ct) => await client.ListOpenApiCollectionsAsync(apiId, after, first, ct)
                ?? throw ThrowHelper.ThereWasAnIssueWithTheRequest("The API was not found."));

        return await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(_title)
            .UseConverter(x => x.Name)
            .RenderAsync(console, cancellationToken);
    }

    public static SelectOpenApiCollectionPrompt New(IOpenApiClient client, string apiId)
        => new(client, apiId);
}
