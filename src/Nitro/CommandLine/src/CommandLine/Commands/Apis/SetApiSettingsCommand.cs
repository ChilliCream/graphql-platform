using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class SetApiSettingsApiCommand : Command
{
    public SetApiSettingsApiCommand() : base("set-settings")
    {
        Description = "Sets the settings of an API";

        AddArgument(Opt<IdArgument>.Instance);
        AddOption(Opt<TreatDangerousAsBreakingOption>.Instance);
        AddOption(Opt<AllowBreakingSchemaChangesOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApisClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApisClient client,
        string id,
        CancellationToken ct)
    {
        console.WriteLine();
        console.WriteLine($"Set settings for API {id.AsHighlight()}");
        console.WriteLine();

        var treatDangerousChangesAsBreaking = await context
            .OptionOrConfirmAsync(
                "Treat dangerous changes as breaking?",
                Opt<TreatDangerousAsBreakingOption>.Instance,
                ct);

        var allowBreakingSchemaChanges = await context
            .OptionOrConfirmAsync(
                "Allow breaking schema changes when no client breaks?",
                Opt<AllowBreakingSchemaChangesOption>.Instance,
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

        context.SetResult(ApiDetailPrompt.From(api).ToObject());

        return ExitCodes.Success;
    }
}
