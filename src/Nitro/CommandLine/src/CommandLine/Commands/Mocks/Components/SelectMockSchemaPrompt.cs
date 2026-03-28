using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Mocks.Components;

public sealed class SelectMockSchemaPrompt(IMocksClient client, string apiId)
{
    private string _title = "Select the mock schema you want to use.";

    public SelectMockSchemaPrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node?> RenderAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        var paginationContainer = PaginationContainer.CreateConnectionData(
            (after, first, ct) => client.ListMockSchemasAsync(apiId, after, first, ct));

        return await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(_title)
            .UseConverter(x => x.Name)
            .RenderAsync(console, cancellationToken);
    }

    public static SelectMockSchemaPrompt New(IMocksClient client, string apiId)
        => new(client, apiId);
}
