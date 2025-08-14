using ChilliCream.Nitro.CLI.Auth;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Exceptions;
using ChilliCream.Nitro.CLI.Option.Binders;
using static ChilliCream.Nitro.CLI.ThrowHelper;

namespace ChilliCream.Nitro.CLI;

internal sealed class SetDefaultWorkspaceCommand : Command
{
    public const string Command = "set-default";

    public SetDefaultWorkspaceCommand() : base(Command)
    {
        Description =
            "Use this command to select a workspace and set it as your default workspace";

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    public static async Task<int> ExecuteAsync(
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

        var selectedWorkspace = await PagedSelectionPrompt
            .New(paginationContainer)
            .Title(message.AsQuestion())
            .UseConverter(x => x.Node.Name)
            .RenderAsync(console, cancellationToken);

        if (selectedWorkspace is null)
        {
            throw Exit("No workspaces was selected as default");
        }

        var workspace = new Workspace(selectedWorkspace.Node.Id, selectedWorkspace.Node.Name);

        await sessionService.SelectWorkspaceAsync(workspace, cancellationToken);

        console.OkQuestion(message, workspace.Name);

        return ExitCodes.Success;
    }
}
