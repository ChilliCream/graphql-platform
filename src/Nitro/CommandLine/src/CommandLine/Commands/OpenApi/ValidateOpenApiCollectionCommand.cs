using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class ValidateOpenApiCollectionCommand : Command
{
    public ValidateOpenApiCollectionCommand(
        INitroConsole console,
        IOpenApiClient client,
        IFileSystem fileSystem) : base("validate")
    {
        Description = "Validate an OpenAPI collection version";

        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<OpenApiCollectionIdOption>.Instance);
        Options.Add(Opt<OpenApiCollectionFilePatternOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                console,
                client,
                fileSystem,
                parseResult.GetValue(Opt<StageNameOption>.Instance)!,
                parseResult.GetValue(Opt<OpenApiCollectionIdOption>.Instance)!,
                parseResult.GetValue(Opt<OpenApiCollectionFilePatternOption>.Instance)!,
                parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance),
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        IOpenApiClient client,
        IFileSystem fileSystem,
        string stage,
        string openApiCollectionId,
        List<string> patterns,
        string? sourceMetadataJson,
        CancellationToken ct)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity("Validating OpenAPI collection..."))
        {
            // console.Log("Searching for OpenAPI documents with the following patterns:");
            // foreach (var pattern in patterns)
            // {
            //     console.Log($"- {pattern}");
            // }

            var files = fileSystem.GlobMatch(patterns, ["**/bin/**", "**/obj/**"]).ToArray();

            if (files.Length < 1)
            {
                activity.Fail();
                throw new ExitException("Could not find any OpenAPI documents with the provided pattern.");
            }

            // console.Log($"Found {files.Length} OpenAPI document(s).");

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

            // console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

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
