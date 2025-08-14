using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Helpers;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Results;
using static ChilliCream.Nitro.CLI.ThrowHelper;

namespace ChilliCream.Nitro.CLI;

internal sealed class ShowWorkspaceCommand : Command
{
    public ShowWorkspaceCommand() : base("show")
    {
        Description = "Shows details of a workspace";

        AddArgument(Opt<IdArgument>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string id,
        CancellationToken cancellationToken)
    {
        var result = await client.ShowWorkspaceCommandQuery.ExecuteAsync(id, cancellationToken);

        var data = result.EnsureData();

        if (data.Node is IWorkspaceDetailPrompt_Workspace node)
        {
            context.SetResult(WorkspaceDetailPrompt.From(node).ToObject());
        }
        else
        {
            throw Exit($"Could not find a workspace with id {id.EscapeMarkup().AsHighlight()}");
        }

        return ExitCodes.Success;
    }
}
