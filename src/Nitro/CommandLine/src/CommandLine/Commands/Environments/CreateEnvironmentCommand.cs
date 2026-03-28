using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Environments;

internal sealed class CreateEnvironmentCommand : Command
{
    public CreateEnvironmentCommand(
        INitroConsole console,
        IEnvironmentsClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("create")
    {
        Description = "Creates a new environment";

        Options.Add(Opt<EnvironmentNameOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IEnvironmentsClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        console.WriteLine();
        console.WriteLine("Creating a environment");
        console.WriteLine();

        var name = await console.PromptAsync(
            "Name",
            defaultValue: null,
            parseResult,
            Opt<EnvironmentNameOption>.Instance,
            cancellationToken);

        var environment = await client.CreateEnvironmentAsync(workspaceId, name, cancellationToken);
        console.PrintMutationErrorsAndExit(environment.Errors);

        var changeResult = environment.Changes?.SingleOrDefault();
        if (changeResult is null)
        {
            throw ThrowHelper.Exit("Could not create environment.");
        }

        if (changeResult.Error is IError error)
        {
            throw ThrowHelper.Exit(error.Message);
        }

        if (changeResult.Result is not IEnvironmentDetailPrompt_Environment detail)
        {
            throw ThrowHelper.Exit("Could not create environment.");
        }

        console.OkLine($"Environment {detail.Name.AsHighlight()} created");

        resultHolder.SetResult(new ObjectResult(EnvironmentDetailPrompt.From(detail).ToObject()));

        return ExitCodes.Success;
    }
}
