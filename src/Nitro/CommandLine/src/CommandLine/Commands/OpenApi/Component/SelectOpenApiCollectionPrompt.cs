using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;

public sealed class SelectOpenApiCollectionPrompt(IApiClient client, string apiId)
{
    private string _title = "Select an OpenAPI collection from the list below.";

    public SelectOpenApiCollectionPrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<ISelectOpenApiCollectionPrompt_OpenApiCollection?> RenderAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        var paginationContainer = PaginationContainer.Create(
            (after, first, ct)
                => client.SelectOpenApiCollectionPromptQuery.ExecuteAsync(apiId, after, first, ct),
            p => p.ApiById?.OpenApiCollections?.PageInfo,
            p => p.ApiById?.OpenApiCollections?.Edges);

        var selectedEdge = await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(_title)
            .UseConverter(x => x.Node.Name)
            .RenderAsync(console, cancellationToken);

        return selectedEdge?.Node;
    }

    public static SelectOpenApiCollectionPrompt New(IApiClient client, string apiId)
        => new(client, apiId);
}
