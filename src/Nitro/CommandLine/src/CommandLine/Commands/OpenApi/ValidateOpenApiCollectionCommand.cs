using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

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

        this.AddExamples(
            """
            openapi validate \
              --openapi-collection-id "<collection-id>" \
              --stage "dev" \
              --pattern "./**/*.graphql"
            """);

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
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var stage = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var openApiCollectionId = parseResult.GetRequiredValue(Opt<OpenApiCollectionIdOption>.Instance);
        var patterns = parseResult.GetRequiredValue(Opt<OpenApiCollectionFilePatternOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var rootActivity = console.StartActivity(
            $"Validating OpenAPI collection '{openApiCollectionId.EscapeMarkup()}' against stage '{stage.EscapeMarkup()}'",
            "Failed to validate the OpenAPI collection."))
        {
            var files = fileSystem.GlobMatch(patterns, ["**/bin/**", "**/obj/**"]).ToArray();

            rootActivity.Update($"Found {files.Length} document(s).");

            if (files.Length < 1)
            {
                rootActivity.Fail();
                throw new ExitException("Could not find any OpenAPI documents with the provided pattern.");
            }

            var archiveStream =
                await OpenApiCollectionHelpers.BuildOpenApiCollectionArchive(
                    fileSystem,
                    files,
                    ct);

            string requestId;

            await using (var child = rootActivity.StartChildActivity(
                Messages.StartingValidationRequest,
                Messages.FailedToStartValidationRequest))
            {
                var validationRequest = await client.StartOpenApiCollectionValidationAsync(
                    openApiCollectionId,
                    stage,
                    archiveStream,
                    source,
                    ct);

                if (validationRequest.Errors?.Count > 0)
                {
                    await child.FailAllAsync();

                    foreach (var error in validationRequest.Errors)
                    {
                        var errorMessage = error switch
                        {
                            IUnauthorizedOperation err => err.Message,
                            IInvalidSourceMetadataInputError err => err.Message,
                            IStageNotFoundError err => err.Message,
                            IOpenApiCollectionNotFoundError err => err.Message,
                            IError err => Messages.UnexpectedMutationError(err),
                            _ => Messages.UnexpectedMutationError()
                        };

                        console.Error.WriteErrorLine(errorMessage);
                    }

                    return ExitCodes.Error;
                }

                if (validationRequest.Id is not { } id)
                {
                    throw new ExitException("Could not create validation request!");
                }

                requestId = id;
                child.Success($"Validation request created (ID: {requestId.EscapeMarkup()}).");
            }

            await using (var child = rootActivity.StartChildActivity(
                Messages.ValidatingActivity,
                Messages.ValidationFailed))
            {
                await foreach (var update in client.SubscribeToOpenApiCollectionValidationAsync(requestId, ct))
                {
                    switch (update)
                    {
                        case IOpenApiCollectionVersionValidationFailed { Errors: var errors }:
                            var errorTree = new Tree("");

                            foreach (var error in errors)
                            {
                                switch (error)
                                {
                                    case IOpenApiCollectionValidationError e:
                                        errorTree.AddOpenApiCollectionValidationErrors(e);
                                        break;
                                    case IOpenApiCollectionValidationArchiveError e:
                                        errorTree.AddErrorMessage(Messages.InvalidArchive(e.Message));
                                        break;
                                    case IUnexpectedProcessingError e:
                                        errorTree.AddErrorMessage(e.Message);
                                        break;
                                    case IProcessingTimeoutError e:
                                        errorTree.AddErrorMessage(e.Message);
                                        break;
                                }
                            }

                            child.Fail(errorTree);

                            await child.FailAllAsync();

                            throw new ExitException("OpenAPI collection validation failed.");

                        case IOpenApiCollectionVersionValidationSuccess:
                            child.Success(Messages.ValidationPassed);
                            rootActivity.Success($"Validated OpenAPI collection against stage '{stage.EscapeMarkup()}'.");
                            return ExitCodes.Success;

                        case IOperationInProgress:
                        case IValidationInProgress:
                            child.Update(Messages.Validating);
                            break;

                        default:
                            child.Update(Messages.UnknownServerResponse, ActivityUpdateKind.Warning);
                            break;
                    }
                }

                child.Fail();
            }
        }

        return ExitCodes.Error;
    }
}
