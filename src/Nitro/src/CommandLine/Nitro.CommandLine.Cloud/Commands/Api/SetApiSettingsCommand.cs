using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Commands.Api.Options;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Results;
using static ChilliCream.Nitro.CLI.ThrowHelper;

namespace ChilliCream.Nitro.CLI.Commands.Api;

internal sealed class SetApiSettingsApiCommand : Command
{
    public SetApiSettingsApiCommand() : base("set-settings")
    {
        Description = "Sets the settings of a api";

        AddArgument(Opt<IdArgument>.Instance);
        AddOption(Opt<TreatDangerousAsBreakingOption>.Instance);
        AddOption(Opt<AllowBreakingSchemaChangesOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string id,
        CancellationToken ct)
    {
        console.WriteLine();
        console.WriteLine($"Set settings for api {id.AsHighlight()}");
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

        var result = await client.SetApiSettingsCommandMutation
            .ExecuteAsync(new UpdateApiSettingsInput
            {
                ApiId = id,
                Settings = new PartialApiSettingsInput()
                {
                    SchemaRegistry = new PartialSchemaRegistrySettingsInput()
                    {
                        TreatDangerousAsBreaking = treatDangerousChangesAsBreaking,
                        AllowBreakingSchemaChanges = allowBreakingSchemaChanges
                    }
                }
            },
                ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.UpdateApiSettings.Errors);

        var api = data.UpdateApiSettings.Api;
        if (api is null)
        {
            throw Exit("Could not update settings.");
        }

        console.OkLine(
            $"Settings of [dim]{string.Join('/', api.Path)}[/]/{api.Name.AsHighlight()} updates");

        if (api is IApiDetailPrompt_Api detail)
        {
            context.SetResult(ApiDetailPrompt.From(detail).ToResult());
        }

        return ExitCodes.Success;
    }
}
