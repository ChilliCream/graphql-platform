using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

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
