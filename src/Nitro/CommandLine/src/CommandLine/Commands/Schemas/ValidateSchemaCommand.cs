using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class ValidateSchemaCommand : Command
{
    public ValidateSchemaCommand() : base("validate")
    {
        Description = "Validate a schema against a stage.";

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<SchemaFileOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            schema validate \
              --api-id "<api-id>" \
              --stage "dev" \
              --schema-file ./schema.graphqls
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<ISchemasClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var stage = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var apiId = parseResult.GetRequiredValue(Opt<ApiIdOption>.Instance);
        var schemaFilePath = parseResult.GetRequiredValue(Opt<SchemaFileOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var rootActivity = console.StartActivity(
            $"Validating schema against stage '{stage.EscapeMarkup()}' of API '{apiId.EscapeMarkup()}'",
            "Failed to validate the schema."))
        {
            string requestId;

            await using (var child = rootActivity.StartChildActivity(
                "Starting validation request",
                "Failed to start the validation request."))
            {
                await using var stream = fileSystem.OpenReadStream(schemaFilePath);

                var validationRequest = await client.StartSchemaValidationAsync(
                    apiId,
                    stage,
                    stream,
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
                            IApiNotFoundError err => err.Message,
                            IStageNotFoundError err => err.Message,
                            ISchemaNotFoundError err => err.Message,
                            IError err => ErrorMessages.UnexpectedMutationError(err),
                            _ => ErrorMessages.UnexpectedMutationError()
                        };

                        console.Error.WriteErrorLine(errorMessage);
                    }

                    return ExitCodes.Error;
                }

                if (validationRequest.Id is not { } id)
                {
                    throw MutationReturnedNoData();
                }

                requestId = id;
                child.Success($"Validation request created (ID: {requestId.EscapeMarkup()}).");
            }

            await using (var child = rootActivity.StartChildActivity(
                "Validating",
                "Validation failed."))
            {
                await foreach (var update in client.SubscribeToSchemaValidationAsync(requestId, ct))
                {
                    switch (update)
                    {
                        case ISchemaVersionValidationFailed { Errors: var schemaErrors }:
                            await child.FailAllAsync();

                            foreach (var error in schemaErrors)
                            {
                                switch (error)
                                {
                                    case ISchemaVersionChangeViolationError e:
                                        console.PrintSchemaVersionChangeViolations(e);
                                        break;
                                    case IInvalidGraphQLSchemaError e:
                                        console.PrintGraphQLSchemaErrors(e);
                                        break;
                                    case IPersistedQueryValidationError e:
                                        console.PrintPersistedQueryValidationErrors(e);
                                        break;
                                    case IOpenApiCollectionValidationError e:
                                        console.PrintOpenApiCollectionValidationErrors(e);
                                        break;
                                    case IMcpFeatureCollectionValidationError e:
                                        console.PrintMcpFeatureCollectionValidationErrors(e);
                                        break;
                                    case IOperationsAreNotAllowedError e:
                                        console.Error.WriteErrorLine(e.Message);
                                        break;
                                    case ISchemaVersionSyntaxError e:
                                        console.Error.WriteErrorLine(e.Message);
                                        break;
                                    case IProcessingTimeoutError e:
                                        console.Error.WriteErrorLine(e.Message);
                                        break;
                                    case IUnexpectedProcessingError e:
                                        console.Error.WriteErrorLine(e.Message);
                                        break;
                                }
                            }

                            console.Error.WriteErrorLine("Schema validation failed.");
                            return ExitCodes.Error;

                        case ISchemaVersionValidationSuccess:
                            child.Success("Validation passed.");
                            rootActivity.Success($"Validated schema against stage '{stage.EscapeMarkup()}'.");

                            return ExitCodes.Success;

                        case IOperationInProgress:
                        case IValidationInProgress:
                            child.Update("Validating...");
                            break;

                        default:
                            child.Update(
                                "Warning: Received an unknown server response. Ensure your CLI is on the latest version.", ActivityUpdateKind.Warning);
                            break;
                    }
                }

                child.Fail();
            }
        }

        return ExitCodes.Error;
    }
}
