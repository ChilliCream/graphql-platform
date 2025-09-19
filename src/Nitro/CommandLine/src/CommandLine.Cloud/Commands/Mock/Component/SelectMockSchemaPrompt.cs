using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Mock.Component;

public sealed class SelectMockSchemaPrompt(IApiClient client, string apiId)
{
    private string _title = "Select the mock schema you want to use.";

    public SelectMockSchemaPrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<IMockSchemaDetailPrompt?> RenderAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        var paginationContainer = PaginationContainer.Create(
            (after, first, ct)
                => client.SelectMockSchemaPromptQuery.ExecuteAsync(apiId, after, first, ct),
            p => p.ApiById?.MockSchemas?.PageInfo,
            p => p.ApiById?.MockSchemas?.Edges);

        var selectedEdge = await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(_title)
            .UseConverter(x => x.Node.Name)
            .RenderAsync(console, cancellationToken);

        return selectedEdge?.Node;
    }

    public static SelectMockSchemaPrompt New(IApiClient client, string apiId)
        => new(client, apiId);
}
