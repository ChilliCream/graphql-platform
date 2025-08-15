using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Auth;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Results;

namespace ChilliCream.Nitro.CLI;

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
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    public static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        CancellationToken ct)
    {
        console.WriteLine();
        console.WriteLine($"Creating a workspace");
        console.WriteLine();

        var name = await context.OptionOrAskAsync("Name", Opt<WorkspaceNameOption>.Instance, ct);

        var asDefault = false;
        var session = sessionService.Session;

        if (console.IsHumandReadable() && session is not null)
        {
            asDefault = await context.OptionOrConfirmAsync(
                "Set as default workspace",
                Opt<SetAsDefaultWorkspaceOption>.Instance,
                ct);
        }

        var input = new CreateWorkspaceInput { Name = name };
        var result =
            await client.CreateWorkspaceCommandMutation.ExecuteAsync(input, ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.CreateWorkspace.Errors);

        if (data.CreateWorkspace.Workspace is not { } createdWorkspace)
        {
            throw new ExitException("Could not create workspace.");
        }

        console.OkLine($"Workspace {createdWorkspace.Name.AsHighlight()} created");

        context.SetResult(WorkspaceDetailPrompt.From(result.Data!.CreateWorkspace.Workspace!)
            .ToObject());

        if (asDefault)
        {
            var workspace = new Workspace(createdWorkspace.Id, createdWorkspace.Name);

            await sessionService.SelectWorkspaceAsync(workspace, ct);

            console.OkLine($"{createdWorkspace.Name.AsHighlight()} set as default workspace");
        }

        return ExitCodes.Success;
    }
}
