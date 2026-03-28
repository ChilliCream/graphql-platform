using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients.Components;

public sealed class SelectClientPrompt(IClientsClient client, string apiId)
{
    private string _title = "Select a client from the list below.";

    public SelectClientPrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<IListClientCommandQuery_Node_Clients_Edges_Node?> RenderAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        var paginationContainer = PaginationContainer.CreateConnectionData(
            (after, first, ct) => client.ListClientsAsync(apiId, after, first, ct));

        return await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(_title)
            .UseConverter(x => x.Name)
            .RenderAsync(console, cancellationToken);
    }

    public static SelectClientPrompt New(IClientsClient client, string apiId)
        => new(client, apiId);
}
