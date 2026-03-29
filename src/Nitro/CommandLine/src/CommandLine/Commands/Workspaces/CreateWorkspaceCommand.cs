using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Components;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

internal sealed class CreateWorkspaceCommand : Command
{
    public CreateWorkspaceCommand(
        INitroConsole console,
        IWorkspacesClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("create")
    {
        Description =
            "Creates a new workspace";

        Options.Add(Opt<SetAsDefaultWorkspaceOption>.Instance);
        Options.Add(Opt<WorkspaceNameOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IWorkspacesClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var name = await console
            .PromptAsync("Name", defaultValue: null, parseResult, Opt<WorkspaceNameOption>.Instance, ct);

        var asDefault = false;
        var session = sessionService.Session;

        if (console.IsInteractive && session is not null)
        {
            asDefault = await parseResult.OptionOrConfirmAsync(
                "Set as default workspace",
                Opt<SetAsDefaultWorkspaceOption>.Instance,
                console,
                ct);
        }

        await using (var activity = console.StartActivity("Creating workspace..."))
        {
            var createdWorkspace = await client.CreateWorkspaceAsync(name, ct);

            if (createdWorkspace.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in createdWorkspace.Errors)
                {
                    var errorMessage = error switch
                    {
                        IUnauthorizedOperation err => err.Message,
                        IValidationError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    await console.Error.WriteLineAsync(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (createdWorkspace.Workspace is not IWorkspaceDetailPrompt_Workspace workspaceDetail)
            {
                activity.Fail();
                await console.Error.WriteLineAsync("Could not create workspace.");
                return ExitCodes.Error;
            }

            activity.Success("Successfully created workspace!");

            resultHolder.SetResult(new ObjectResult(WorkspaceDetailPrompt.From(workspaceDetail).ToObject()));

            if (asDefault)
            {
                var workspace = new Workspace(workspaceDetail.Id, workspaceDetail.Name);

                await sessionService.SelectWorkspaceAsync(workspace, ct);

                console.OkLine($"{workspaceDetail.Name.AsHighlight()} set as default workspace");
            }

            return ExitCodes.Success;
        }
    }
}
