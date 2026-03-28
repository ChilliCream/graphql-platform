using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Components;

public sealed class SelectApiPrompt(IApisClient client, string workspaceId)
{
    private string _title = "Select the API you want to use.";

    public SelectApiPrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<ISelectApiPromptQuery_WorkspaceById_Apis_Edges_Node?> RenderAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        var paginationContainer = PaginationContainer.CreateConnectionData(
            (after, first, ct)
                => client.SelectApisAsync(workspaceId, after, first, ct));

        return await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(_title)
            .UseConverter(x => x.Name)
            .RenderAsync(console, cancellationToken);
    }

    public static SelectApiPrompt New(IApisClient client, string workspaceId)
        => new(client, workspaceId);
}
