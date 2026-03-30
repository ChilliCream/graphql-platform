using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
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

        await using (var activity = console.StartActivity($"Validating OpenAPI collection against stage '{stage.EscapeMarkup()}'"))
        {
            // console.Log("Searching for OpenAPI documents with the following patterns:");
            // foreach (var pattern in patterns)
            // {
            //     console.Log($"- {pattern}");
            // }

            var files = fileSystem.GlobMatch(patterns, ["**/bin/**", "**/obj/**"]).ToArray();

            if (files.Length < 1)
            {
                activity.Fail("Failed to validate the OpenAPI collection.");
                throw new ExitException("Could not find any OpenAPI documents with the provided pattern.");
            }

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

            if (validationRequest.Errors?.Count > 0)
            {
                activity.Fail("Failed to validate the OpenAPI collection.");

                foreach (var error in validationRequest.Errors)
                {
                    var errorMessage = error switch
                    {
                        IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors_UnauthorizedOperation err => err.Message,
                        IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors_StageNotFoundError err => err.Message,
                        IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors_OpenApiCollectionNotFoundError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    await console.Error.WriteLineAsync(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (validationRequest.Id is not { } requestId)
            {
                throw new ExitException("Could not create validation request!");
            }

            activity.Update($"Validation request created (ID: {requestId.EscapeMarkup()})");

            await foreach (var update in client.SubscribeToOpenApiCollectionValidationAsync(requestId, ct))
            {
                switch (update)
                {
                    case IOpenApiCollectionVersionValidationFailed { Errors: var errors }:
                        activity.Fail("Failed to validate the OpenAPI collection.");

                        foreach (var error in errors)
                        {
                            switch (error)
                            {
                                case IUnexpectedProcessingError e:
                                    await console.Error.WriteLineAsync(e.Message);
                                    break;
                                case IProcessingTimeoutError e:
                                    await console.Error.WriteLineAsync(e.Message);
                                    break;
                                case IOpenApiCollectionValidationError e:
                                    console.PrintOpenApiCollectionValidationErrors(e);
                                    break;
                                case IOpenApiCollectionValidationArchiveError e:
                                    await console.Error.WriteLineAsync(e.Message);
                                    break;
                                case IError e:
                                    await console.Error.WriteLineAsync("Unexpected error: " + e.Message);
                                    break;
                            }
                        }

                        await console.Error.WriteLineAsync("OpenAPI collection validation failed.");
                        return ExitCodes.Error;

                    case IOpenApiCollectionVersionValidationSuccess:
                        activity.Success("Validated the OpenAPI collection.");
                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update("Validating...");
                        break;

                    default:
                        activity.Warning("Unknown server response. Consider updating the CLI.");
                        break;
                }
            }

            activity.Fail("Failed to validate the OpenAPI collection.");
        }

        return ExitCodes.Error;
    }
}
