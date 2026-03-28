using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

internal sealed class ShowWorkspaceCommand : Command
{
    public ShowWorkspaceCommand(
        IWorkspacesClient client,
        IResultHolder resultHolder) : base("show")
    {
        Description = "Shows details of a workspace";

        Arguments.Add(Opt<IdArgument>.Instance);

        SetAction((parseResult, cancellationToken)
            => ExecuteAsync(parseResult, client, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        IWorkspacesClient client,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        var id = parseResult.GetValue(Opt<IdArgument>.Instance)!;
        var data = await client.ShowWorkspaceAsync(id, cancellationToken);

        if (data is IShowWorkspaceCommandQuery_Node_Workspace node)
        {
            resultHolder.SetResult(new ObjectResult(WorkspaceDetailPrompt.From(node).ToObject()));
        }
        else
        {
            throw Exit($"Could not find a workspace with ID {id.EscapeMarkup().AsHighlight()}");
        }

        return ExitCodes.Success;
    }
}
