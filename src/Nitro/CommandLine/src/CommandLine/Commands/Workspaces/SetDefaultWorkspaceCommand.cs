using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

internal sealed class SetDefaultWorkspaceCommand : Command
{
    public const string Command = "set-default";

    public SetDefaultWorkspaceCommand() : base(Command)
    {
        Description =
            "Set the default workspace.";

        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("workspace set-default");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IWorkspacesClient>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var workspaceId = parseResult.GetValue(Opt<OptionalWorkspaceIdOption>.Instance);

        if (workspaceId is not null)
        {
            var data = await client.GetWorkspaceAsync(workspaceId, cancellationToken);

            if (data is not IShowWorkspaceCommandQuery_Node_Workspace node)
            {
                throw Exit($"The workspace with ID '{workspaceId}' was not found.");
            }

            var workspace = new Workspace(node.Id, node.Name);
            await sessionService.SelectWorkspaceAsync(workspace, cancellationToken);
            return ExitCodes.Success;
        }

        if (!console.IsInteractive)
        {
            throw MissingRequiredOption(OptionalWorkspaceIdOption.OptionName);
        }

        return await ExecuteAsync(true, console, client, sessionService, cancellationToken);
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
                throw Exit("No workspace was selected as default.");
            }

            workspace = new Workspace(selectedWorkspace.Id, selectedWorkspace.Name);

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
