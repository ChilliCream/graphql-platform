using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Environments;

internal sealed class CreateEnvironmentCommand : Command
{
    public CreateEnvironmentCommand() : base("create")
    {
        Description = "Create a new environment.";

        Options.Add(Opt<EnvironmentNameOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("environment create --name \"dev\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IEnvironmentsClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        var name = await console.PromptAsync(
            "Name",
            defaultValue: null,
            parseResult,
            Opt<EnvironmentNameOption>.Instance,
            cancellationToken);

        await using (var activity = console.StartActivity(
            $"Creating environment '{name.EscapeMarkup()}'",
            "Failed to create the environment."))
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
                        IError err => Messages.UnexpectedMutationError(err),
                        _ => Messages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            var changeResult = data.Changes?.SingleOrDefault();
            if (changeResult is null)
            {
                throw MutationReturnedNoData();
            }

            if (changeResult.Error is IError changeError)
            {
                activity.Fail();
                console.Error.WriteErrorLine(changeError.Message);
                return ExitCodes.Error;
            }

            if (changeResult.Result is not IEnvironmentDetailPrompt_Environment detail)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Created environment '{name.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(EnvironmentDetailPrompt.From(detail).ToObject()));

            return ExitCodes.Success;
        }
    }
}
