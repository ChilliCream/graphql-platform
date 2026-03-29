using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class SetApiSettingsApiCommand : Command
{
    public SetApiSettingsApiCommand(
        INitroConsole console,
        IApisClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("set-settings")
    {
        Description = "Sets the settings of an API";

        Arguments.Add(Opt<IdArgument>.Instance);
        Options.Add(Opt<TreatDangerousAsBreakingOption>.Instance);
        Options.Add(Opt<AllowBreakingSchemaChangesOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var id = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        var treatDangerousChangesAsBreaking = await console
            .ConfirmAsync(
                parseResult,
                Opt<TreatDangerousAsBreakingOption>.Instance,
                "Treat dangerous changes as breaking?",
                ct);

        var allowBreakingSchemaChanges = await console
            .ConfirmAsync(
                parseResult,
                Opt<AllowBreakingSchemaChangesOption>.Instance,
                "Allow breaking schema changes when no client breaks?",
                ct);

        await using var activity = console.StartActivity("Updating API settings...");

        var data = await client.UpdateApiSettingsAsync(
            id,
            treatDangerousChangesAsBreaking,
            allowBreakingSchemaChanges,
            ct);

        if (data.Errors?.Count > 0)
        {
            activity.Fail();

            foreach (var mutationError in data.Errors)
            {
                var errorMessage = mutationError switch
                {
                    ISetApiSettingsCommandMutation_UpdateApiSettings_Errors_ApiNotFoundError err => err.Message,
                    ISetApiSettingsCommandMutation_UpdateApiSettings_Errors_UnauthorizedOperation err => err.Message,
                    IError err => "Unexpected mutation error: " + err.Message,
                    _ => "Unexpected mutation error."
                };

                await console.Error.WriteLineAsync(errorMessage);
                return ExitCodes.Error;
            }
        }

        if (data.Api is not IApiDetailPrompt_Api api)
        {
            activity.Fail();
            await console.Error.WriteLineAsync("Could not update settings.");
            return ExitCodes.Error;
        }

        activity.Success("Successfully updated API settings!");

        resultHolder.SetResult(new ObjectResult(ApiDetailPrompt.From(api).ToObject()));

        return ExitCodes.Success;
    }
}
