using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class ValidateMcpFeatureCollectionCommand : Command
{
    public ValidateMcpFeatureCollectionCommand() : base("validate")
    {
        Description = "Validate an MCP Feature Collection version";

        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<McpFeatureCollectionIdOption>.Instance);
        AddOption(Opt<McpPromptFilePatternOption>.Instance);
        AddOption(Opt<McpToolFilePatternOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<StageNameOption>.Instance,
            Opt<McpFeatureCollectionIdOption>.Instance,
            Opt<McpPromptFilePatternOption>.Instance,
            Opt<McpToolFilePatternOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string stage,
        string mcpFeatureCollectionId,
        List<string> promptPatterns,
        List<string> toolPatterns,
        CancellationToken ct)
    {
        console.Title($"Validate against {stage.EscapeMarkup()}");

        var isValid = false;

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Validating...", ValidateMcpFeatureCollection);
        }
        else
        {
            await ValidateMcpFeatureCollection(null);
        }

        return isValid ? ExitCodes.Success : ExitCodes.Error;

        async Task ValidateMcpFeatureCollection(StatusContext? ctx)
        {
            console.Log("Searching for MCP prompt definition files with the following patterns:");
            foreach (var promptPattern in promptPatterns)
            {
                console.Log($"- {promptPattern}");
            }

            console.Log("Searching for MCP tool definition files with the following patterns:");
            foreach (var toolPattern in toolPatterns)
            {
                console.Log($"- {toolPattern}");
            }

            var promptFiles = GlobMatcher.Match(promptPatterns).ToArray();
            var toolFiles = GlobMatcher.Match(toolPatterns).ToArray();

            if (promptFiles.Length < 1 && toolFiles.Length < 1)
            {
                console.WriteLine("Could not find any MCP prompt or tool definition files with the provided patterns.");
                return;
            }

            console.Log($"Found {promptFiles.Length} MCP prompt definition file(s).");
            console.Log($"Found {toolFiles.Length} MCP tool definition file(s).");

            var archiveStream =
                await McpFeatureCollectionHelpers.BuildMcpFeatureCollectionArchive(promptFiles, toolFiles, ct);

            var input = new ValidateMcpFeatureCollectionInput
            {
                McpFeatureCollectionId = mcpFeatureCollectionId,
                Stage = stage,
                Collection = new Upload(archiveStream, "collection.zip")
            };

            var requestId = await ValidateAsync(console, client, input, ct);

            console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            using var stopSignal = new Subject<Unit>();

            var subscription = client.ValidateMcpFeatureCollectionCommandSubscription
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            await foreach (var x in subscription.ToAsyncEnumerable().WithCancellation(ct))
            {
                if (x.Errors is { Count: > 0 } errors)
                {
                    console.PrintErrorsAndExit(errors);
                    throw Exit("No request id returned");
                }

                switch (x.Data?.OnMcpFeatureCollectionVersionValidationUpdate)
                {
                    case IMcpFeatureCollectionVersionValidationFailed { Errors: var validationErrors }:
                        console.ErrorLine("The MCP Feature Collection is invalid:");
                        console.PrintErrorsAndExit(validationErrors);
                        stopSignal.OnNext(Unit.Default);
                        break;

                    case IMcpFeatureCollectionVersionValidationSuccess:
                        isValid = true;
                        stopSignal.OnNext(Unit.Default);
                        console.Success("MCP Feature Collection validation succeeded");
                        break;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        ctx?.Status("The validation is in progress.");
                        break;

                    default:
                        ctx?.Status(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }
    }

    private static async Task<string> ValidateAsync(
        IAnsiConsole console,
        IApiClient client,
        ValidateMcpFeatureCollectionInput input,
        CancellationToken ct)
    {
        var result =
            await client.ValidateMcpFeatureCollectionCommandMutation.ExecuteAsync(input, ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.ValidateMcpFeatureCollection.Errors);

        if (data.ValidateMcpFeatureCollection.Id is null)
        {
            throw new ExitException("Could not create validation request!");
        }

        return data.ValidateMcpFeatureCollection.Id;
    }
}
