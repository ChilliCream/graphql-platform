using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class ValidateOpenApiCollectionCommand : Command
{
    public ValidateOpenApiCollectionCommand() : base("validate")
    {
        Description = "Validate an OpenAPI collection version.";

        Options.Add(Opt<OpenApiCollectionIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<OpenApiCollectionFilePatternOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IOpenApiClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();

        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var openApiCollectionId = parseResult.GetValue(Opt<OpenApiCollectionIdOption>.Instance)!;
        var patterns = parseResult.GetValue(Opt<OpenApiCollectionFilePatternOption>.Instance)!;
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity(
            $"Validating OpenAPI collection against stage '{stage.EscapeMarkup()}'",
            "Failed to validate the OpenAPI collection."))
        {
            var files = fileSystem.GlobMatch(patterns, ["**/bin/**", "**/obj/**"]).ToArray();

            activity.Update($"Found {files.Length} document(s).");

            if (files.Length < 1)
            {
                activity.Fail();
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
                activity.Fail();

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

                    console.Error.WriteErrorLine(errorMessage);
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
                        activity.Fail();

                        foreach (var error in errors)
                        {
                            switch (error)
                            {
                                case IUnexpectedProcessingError e:
                                    console.Error.WriteErrorLine(e.Message);
                                    break;
                                case IProcessingTimeoutError e:
                                    console.Error.WriteErrorLine(e.Message);
                                    break;
                                case IOpenApiCollectionValidationError e:
                                    console.PrintOpenApiCollectionValidationErrors(e);
                                    break;
                                case IOpenApiCollectionValidationArchiveError e:
                                    console.Error.WriteErrorLine(ErrorMessages.InvalidArchive(e.Message));
                                    break;
                                case IError e:
                                    console.Error.WriteErrorLine("Unexpected error: " + e.Message);
                                    break;
                            }
                        }

                        console.Error.WriteErrorLine("OpenAPI collection validation failed.");
                        return ExitCodes.Error;

                    case IOpenApiCollectionVersionValidationSuccess:
                        activity.Success($"Validated OpenAPI collection against stage '{stage.EscapeMarkup()}'.");
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

            activity.Fail();
        }

        return ExitCodes.Error;
    }
}
