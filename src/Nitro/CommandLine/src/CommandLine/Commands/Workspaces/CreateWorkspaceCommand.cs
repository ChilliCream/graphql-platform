using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Components;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

internal sealed class CreateWorkspaceCommand : Command
{
    public CreateWorkspaceCommand() : base("create")
    {
        Description =
            "Creates a new workspace";

        AddOption(Opt<SetAsDefaultWorkspaceOption>.Instance);
        AddOption(Opt<WorkspaceNameOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IWorkspacesClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    public static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IWorkspacesClient client,
        ISessionService sessionService,
        CancellationToken ct)
    {
        console.WriteLine();
        console.WriteLine("Creating a workspace");
        console.WriteLine();

        var name = await context.OptionOrAskAsync("Name", Opt<WorkspaceNameOption>.Instance, ct);

        var asDefault = false;
        var session = sessionService.Session;

        if (console.IsInteractive && session is not null)
        {
            asDefault = await context.OptionOrConfirmAsync(
                "Set as default workspace",
                Opt<SetAsDefaultWorkspaceOption>.Instance,
                ct);
        }

        var createdWorkspace = await client.CreateWorkspaceAsync(name, ct);
        console.PrintMutationErrorsAndExit(createdWorkspace.Errors);

        if (createdWorkspace.Workspace is not IWorkspaceDetailPrompt_Workspace workspaceDetail)
        {
            throw new ExitException("Could not create workspace.");
        }

        console.OkLine($"Workspace {workspaceDetail.Name.AsHighlight()} created");

        context.SetResult(WorkspaceDetailPrompt.From(workspaceDetail).ToObject());

        if (asDefault)
        {
            var workspace = new Workspace(workspaceDetail.Id, workspaceDetail.Name);

            await sessionService.SelectWorkspaceAsync(workspace, ct);

            console.OkLine($"{workspaceDetail.Name.AsHighlight()} set as default workspace");
        }

        return ExitCodes.Success;
    }
}
