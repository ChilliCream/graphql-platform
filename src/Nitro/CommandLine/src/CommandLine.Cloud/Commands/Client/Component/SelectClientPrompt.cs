using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud;

public sealed class SelectClientPrompt(IApiClient client, string apiId)
{
    private string _title = "Select a client from the list below.";

    public SelectClientPrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<ISelectClientPrompt_Client?> RenderAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        var paginationContainer = PaginationContainer.Create(
            (after, first, ct)
                => client.SelectClientPromptQuery.ExecuteAsync(apiId, after, first, ct),
            p => p.ApiById?.Clients?.PageInfo,
            p => p.ApiById?.Clients?.Edges);

        var selectedEdge = await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(_title)
            .UseConverter(x => x.Node.Name)
            .RenderAsync(console, cancellationToken);

        return selectedEdge?.Node;
    }

    public static SelectClientPrompt New(IApiClient client, string apiId)
        => new(client, apiId);
}
