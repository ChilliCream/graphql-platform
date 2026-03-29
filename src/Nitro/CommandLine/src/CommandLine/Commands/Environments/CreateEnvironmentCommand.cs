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
        parseResult.AssertHasAuthentication(sessionService);

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        var name = await console.PromptAsync(
            "Name",
            defaultValue: null,
            parseResult,
            Opt<EnvironmentNameOption>.Instance,
            cancellationToken);

        await using (var activity = console.StartActivity("Creating environment..."))
        {
            var data = await client.CreateEnvironmentAsync(workspaceId, name, cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Errors_UnauthorizedOperation err => err.Message,
                        ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Errors_ChangeStructureInvalid err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    await console.Error.WriteLineAsync(errorMessage);
                    return ExitCodes.Error;
                }
            }

            var changeResult = data.Changes?.SingleOrDefault();
            if (changeResult is null)
            {
                activity.Fail();
                await console.Error.WriteLineAsync("Could not create environment.");
                return ExitCodes.Error;
            }

            if (changeResult.Error is IError changeError)
            {
                activity.Fail();
                await console.Error.WriteLineAsync(changeError.Message);
                return ExitCodes.Error;
            }

            if (changeResult.Result is not IEnvironmentDetailPrompt_Environment detail)
            {
                activity.Fail();
                await console.Error.WriteLineAsync("Could not create environment.");
                return ExitCodes.Error;
            }

            activity.Success("Successfully created environment!");

            console.WriteLine();

            resultHolder.SetResult(new ObjectResult(EnvironmentDetailPrompt.From(detail).ToObject()));

            return ExitCodes.Success;
        }
    }
}
