using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class SetApiSettingsApiCommand : Command
{
    public SetApiSettingsApiCommand() : base("set-settings")
    {
        Description = "Set the settings of an API.";

        Arguments.Add(Opt<IdArgument>.Instance);
        Options.Add(Opt<TreatDangerousAsBreakingOption>.Instance);
        Options.Add(Opt<AllowBreakingSchemaChangesOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(async (services, parseResult, cancellationToken) =>
        {
            var console = services.GetRequiredService<INitroConsole>();
            var client = services.GetRequiredService<IApisClient>();
            var sessionService = services.GetRequiredService<ISessionService>();
            var resultHolder = services.GetRequiredService<IResultHolder>();
            return await ExecuteAsync(parseResult, console, client, sessionService, resultHolder, cancellationToken);
        });
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

        await using var activity = console.StartActivity(
            $"Updating settings for API '{id.EscapeMarkup()}'",
            "Failed to update the API settings.");

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

                console.Error.WriteErrorLine(errorMessage);
                return ExitCodes.Error;
            }
        }

        if (data.Api is not IApiDetailPrompt_Api api)
        {
            throw MutationReturnedNoData();
        }

        activity.Success($"Updated settings for API '{id.EscapeMarkup()}'.");

        resultHolder.SetResult(new ObjectResult(ApiDetailPrompt.From(api).ToObject()));

        return ExitCodes.Success;
    }
}
