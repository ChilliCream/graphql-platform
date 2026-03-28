using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class SetApiSettingsApiCommand : Command
{
    public SetApiSettingsApiCommand(
        INitroConsole console,
        IApisClient client,
        IResultHolder resultHolder) : base("set-settings")
    {
        Description = "Sets the settings of an API";

        Arguments.Add(Opt<IdArgument>.Instance);
        Options.Add(Opt<TreatDangerousAsBreakingOption>.Instance);
        Options.Add(Opt<AllowBreakingSchemaChangesOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var id = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        console.WriteLine($"Set settings for API {id.AsHighlight()}");

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

        var data = await client.UpdateApiSettingsAsync(
            id,
            treatDangerousChangesAsBreaking,
            allowBreakingSchemaChanges,
            ct);

        console.PrintMutationErrorsAndExit(data.Errors);

        if (data.Api is not IApiDetailPrompt_Api api)
        {
            throw ThrowHelper.Exit("Could not update settings.");
        }

        console.OkLine(
            $"Settings of [dim]{string.Join('/', api.Path)}[/]/{api.Name.AsHighlight()} updated");

        resultHolder.SetResult(new ObjectResult(ApiDetailPrompt.From(api).ToObject()));

        return ExitCodes.Success;
    }
}
