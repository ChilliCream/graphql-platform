using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Components;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

internal sealed class CreateWorkspaceCommand : Command
{
    public CreateWorkspaceCommand() : base("create")
    {
        Description =
            "Create a new workspace.";

        Options.Add(Opt<WorkspaceNameOption>.Instance);
        Options.Add(Opt<SetAsDefaultWorkspaceOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("workspace create --name \"my-workspace\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IWorkspacesClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var name = await console
            .PromptAsync("Name", defaultValue: null, parseResult, Opt<WorkspaceNameOption>.Instance, ct);

        var asDefault = await console
            .ConfirmAsync(
                parseResult,
                Opt<SetAsDefaultWorkspaceOption>.Instance,
                "Set as default workspace?",
                ct);

        await using (var activity = console.StartActivity(
            $"Creating workspace '{name.EscapeMarkup()}'",
            "Failed to create the workspace."))
        {
            var createdWorkspace = await client.CreateWorkspaceAsync(name, ct);

            if (createdWorkspace.Errors?.Count > 0)
            {
                await activity.FailAllAsync();

                foreach (var error in createdWorkspace.Errors)
                {
                    var errorMessage = error switch
                    {
                        IUnauthorizedOperation err => err.Message,
                        IValidationError err => err.Message,
                        IError err => Messages.UnexpectedMutationError(err),
                        _ => Messages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (createdWorkspace.Workspace is not IWorkspaceDetailPrompt_Workspace workspaceDetail)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Created workspace '{name.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(WorkspaceDetailPrompt.From(workspaceDetail).ToObject()));

            if (asDefault)
            {
                var workspace = new Workspace(workspaceDetail.Id, workspaceDetail.Name);

                await sessionService.SelectWorkspaceAsync(workspace, ct);

                console.WriteLine($"{workspaceDetail.Name.EscapeMarkup()} set as default workspace");
            }

            return ExitCodes.Success;
        }
    }
}
