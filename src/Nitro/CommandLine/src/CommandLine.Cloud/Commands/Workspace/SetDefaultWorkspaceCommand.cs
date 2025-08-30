using ChilliCream.Nitro.CommandLine.Cloud.Auth;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class SetDefaultWorkspaceCommand : Command
{
    public const string Command = "set-default";

    public SetDefaultWorkspaceCommand() : base(Command)
    {
        Description =
            "Use this command to select a workspace and set it as your default workspace";

        this.SetHandler(context => ExecuteAsync(
            true,
            context.BindingContext.GetRequiredService<IAnsiConsole>(),
            context.BindingContext.GetRequiredService<IApiClient>(),
            context.BindingContext.GetRequiredService<ISessionService>(),
            context.BindingContext.GetRequiredService<CancellationToken>()));
    }

    public static async Task<int> ExecuteAsync(
        bool forceSelection,
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        const string message = "Which workspace do you want to use as your default?";

        var paginationContainer = PaginationContainer.Create(
            client.SetDefaultWorkspaceCommand_SelectWorkspace_Query.ExecuteAsync,
            p => p.Me?.Workspaces?.PageInfo,
            p => p.Me?.Workspaces?.Edges);

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
            var firstWorkspace = current[0].Node;
            workspace = new Workspace(firstWorkspace.Id, firstWorkspace.Name);
        }
        else
        {
             var selectedWorkspace = await PagedSelectionPrompt
                .New(paginationContainer)
                .Title(message.AsQuestion())
                .UseConverter(x => x.Node.Name)
                .RenderAsync(console, cancellationToken);

             if (selectedWorkspace is null)
             {
                 throw Exit("No workspaces was selected as default");
             }

             workspace = new Workspace(selectedWorkspace.Node.Id, selectedWorkspace.Node.Name);

            wasPrompted = true;
        }

        await sessionService.SelectWorkspaceAsync(workspace, cancellationToken);

        if (wasPrompted)
        {
            console.OkQuestion(message, workspace.Name);
        }

        return ExitCodes.Success;
    }
}
