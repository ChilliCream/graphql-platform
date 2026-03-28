using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class ValidateOpenApiCollectionCommand : Command
{
    public ValidateOpenApiCollectionCommand() : base("validate")
    {
        Description = "Validate an OpenAPI collection version";

        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<OpenApiCollectionIdOption>.Instance);
        AddOption(Opt<OpenApiCollectionFilePatternOption>.Instance);
        AddOption(Opt<OptionalSourceMetadataOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IOpenApiClient>(),
            Bind.FromServiceProvider<IFileSystem>(),
            Opt<StageNameOption>.Instance,
            Opt<OpenApiCollectionIdOption>.Instance,
            Opt<OpenApiCollectionFilePatternOption>.Instance,
            Opt<OptionalSourceMetadataOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IOpenApiClient client,
        IFileSystem fileSystem,
        string stage,
        string openApiCollectionId,
        List<string> patterns,
        string? sourceMetadataJson,
        CancellationToken ct)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity("Validating..."))
        {
            console.Log("Searching for OpenAPI documents with the following patterns:");
            foreach (var pattern in patterns)
            {
                console.Log($"- {pattern}");
            }

            var files = fileSystem.GlobMatch(patterns, ["**/bin/**", "**/obj/**"]).ToArray();

            if (files.Length < 1)
            {
                console.WriteLine("Could not find any OpenAPI documents with the provided pattern.");
                return ExitCodes.Error;
            }

            console.Log($"Found {files.Length} OpenAPI document(s).");

            var archiveStream =
                await OpenApiCollectionHelpers.BuildOpenApiCollectionArchive(
                    fileSystem,
                    files,
                    ct);

            var validationRequest = await client.StartOpenApiCollectionValidationAsync(
                openApiCollectionId,
                stage,
                archiveStream,
                source,
                ct);

            console.PrintMutationErrorsAndExit(validationRequest.Errors);
            if (validationRequest.Id is not { } requestId)
            {
                throw new ExitException("Could not create validation request!");
            }

            console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            await foreach (var update in client.SubscribeToOpenApiCollectionValidationAsync(requestId, ct))
            {
                switch (update)
                {
                    case IOpenApiCollectionVersionValidationFailed { Errors: var errors }:
                        console.WriteLine("The OpenAPI collection is invalid:");
                        console.PrintMutationErrors(errors);
                        return ExitCodes.Error;

                    case IOpenApiCollectionVersionValidationSuccess:
                        console.Success("OpenAPI collection validation succeeded");
                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update("The validation is in progress.");
                        break;

                    default:
                        activity.Update(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }

        return ExitCodes.Error;
    }
}
