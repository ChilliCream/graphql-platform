using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

internal sealed class SetDefaultWorkspaceCommand : Command
{
    public const string Command = "set-default";

    public SetDefaultWorkspaceCommand() : base(Command)
    {
        Description =
            "Use this command to select a workspace and set it as your default workspace";

        this.SetHandler(context => ExecuteAsync(
            true,
            context.BindingContext.GetRequiredService<INitroConsole>(),
            context.BindingContext.GetRequiredService<IWorkspacesClient>(),
            context.BindingContext.GetRequiredService<ISessionService>(),
            context.BindingContext.GetRequiredService<CancellationToken>()));
    }

    public static async Task<int> ExecuteAsync(
        bool forceSelection,
        INitroConsole console,
        IWorkspacesClient client,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        const string message = "Which workspace do you want to use as your default?";

        var paginationContainer = PaginationContainer.CreateConnectionData(client.SelectWorkspacesAsync);

        var current = await paginationContainer.GetCurrentAsync(cancellationToken);
        if (current.Count == 0)
        {
            throw new ExitException(
                $"You do not have any workspaces. Run {"nitro launch".AsCommand()} and create one.");
        }

        Workspace? workspace;
        var wasPrompted = false;

        if (current.Count == 1 && !forceSelection)
        {
            var firstWorkspace = current[0];
            workspace = new Workspace(firstWorkspace.Id, firstWorkspace.Name);
        }
        else
        {
            var selectedWorkspace = await PagedSelectionPrompt
                .New(paginationContainer)
                .Title(message.AsQuestion())
                .UseConverter(x => x.Name)
                .RenderAsync(console, cancellationToken);

            if (selectedWorkspace is null)
            {
                throw Exit("No workspaces was selected as default");
            }

            workspace = new Workspace(selectedWorkspace.Id, selectedWorkspace.Name);

            wasPrompted = true;
        }

        await sessionService.SelectWorkspaceAsync(workspace, cancellationToken);

        if (wasPrompted)
        {
            // console.OkQuestion(message, workspace.Name);
        }

        return ExitCodes.Success;
    }
}
