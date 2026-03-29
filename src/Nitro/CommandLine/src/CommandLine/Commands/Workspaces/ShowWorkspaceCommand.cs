using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

internal sealed class ShowWorkspaceCommand : Command
{
    public ShowWorkspaceCommand(
        INitroConsole console,
        IWorkspacesClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("show")
    {
        Description = "Shows details of a workspace";

        Arguments.Add(Opt<IdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        IWorkspacesClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var id = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        var data = await client.ShowWorkspaceAsync(id, cancellationToken);

        if (data is IShowWorkspaceCommandQuery_Node_Workspace node)
        {
            resultHolder.SetResult(new ObjectResult(WorkspaceDetailPrompt.From(node).ToObject()));
            return ExitCodes.Success;
        }

        throw Exit($"The workspace with ID '{id}' was not found.");
    }
}
