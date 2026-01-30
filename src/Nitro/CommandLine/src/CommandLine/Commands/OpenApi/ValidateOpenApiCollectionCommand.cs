using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class ValidateOpenApiCollectionCommand : Command
{
    public ValidateOpenApiCollectionCommand() : base("validate")
    {
        Description = "Validate an OpenAPI collection version";

        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<OpenApiCollectionIdOption>.Instance);
        AddOption(Opt<OpenApiCollectionFilePatternOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<StageNameOption>.Instance,
            Opt<OpenApiCollectionIdOption>.Instance,
            Opt<OpenApiCollectionFilePatternOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string stage,
        string openApiCollectionId,
        List<string> patterns,
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
                .StartAsync("Validating...", ValidateOpenApiCollection);
        }
        else
        {
            await ValidateOpenApiCollection(null);
        }

        return isValid ? ExitCodes.Success : ExitCodes.Error;

        async Task ValidateOpenApiCollection(StatusContext? ctx)
        {
            console.Log("Searching for OpenAPI documents with the following patterns:");
            foreach (var pattern in patterns)
            {
                console.Log($"- {pattern}");
            }

            var files = GlobMatcher.Match(patterns).ToArray();

            if (files.Length < 1)
            {
                console.WriteLine("Could not find any OpenAPI documents with the provided pattern.");
                return;
            }

            console.Log($"Found {files.Length} OpenAPI document(s).");

            var archiveStream =
                await OpenApiCollectionHelpers.BuildOpenApiCollectionArchive(files, ct);

            var input = new ValidateOpenApiCollectionInput
            {
                OpenApiCollectionId = openApiCollectionId,
                Stage = stage,
                Collection = new Upload(archiveStream, "collection.zip")
            };

            var requestId = await ValidateAsync(console, client, input, ct);

            console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            using var stopSignal = new Subject<Unit>();

            var subscription = client.ValidateOpenApiCollectionCommandSubscription
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            await foreach (var x in subscription.ToAsyncEnumerable().WithCancellation(ct))
            {
                if (x.Errors is { Count: > 0 } errors)
                {
                    console.PrintErrorsAndExit(errors);
                    throw Exit("No request id returned");
                }

                switch (x.Data?.OnOpenApiCollectionVersionValidationUpdate)
                {
                    case IOpenApiCollectionVersionValidationFailed { Errors: var validationErrors }:
                        console.WriteLine("The OpenAPI collection is invalid:");
                        console.PrintErrorsAndExit(validationErrors);
                        stopSignal.OnNext(Unit.Default);
                        break;

                    case IOpenApiCollectionVersionValidationSuccess:
                        isValid = true;
                        stopSignal.OnNext(Unit.Default);
                        console.Success("OpenAPI collection validation succeeded");
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
        ValidateOpenApiCollectionInput input,
        CancellationToken ct)
    {
        var result =
            await client.ValidateOpenApiCollectionCommandMutation.ExecuteAsync(input, ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.ValidateOpenApiCollection.Errors);

        if (data.ValidateOpenApiCollection.Id is null)
        {
            throw new ExitException("Could not create validation request!");
        }

        return data.ValidateOpenApiCollection.Id;
    }
}
