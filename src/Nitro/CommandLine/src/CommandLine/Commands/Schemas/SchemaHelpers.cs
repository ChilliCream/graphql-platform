using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal static class SchemaHelpers
{
    public static async Task<bool> ValidateSchemaAsync(
        INitroConsoleActivity activity,
        INitroConsole console,
        ISchemasClient client,
        string apiId,
        string stageName,
        Stream schema,
        SourceMetadata? source,
        CancellationToken ct)
    {
        var result = await client.StartSchemaValidationAsync(
            apiId,
            stageName,
            schema,
            source,
            ct);

        if (result.Errors?.Count > 0)
        {
            await activity.FailAllAsync();

            foreach (var error in result.Errors)
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

            throw new ExitException();
        }

        var requestId = result.Id;

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ExitException("Could not create validation request!");
        }

        activity.Update($"Validation request created (ID: {requestId.EscapeMarkup()}).");

        await foreach (var @event in client.SubscribeToSchemaValidationAsync(requestId, ct))
        {
            switch (@event)
            {
                case ISchemaVersionValidationFailed { Errors: var schemaErrors }:
                    var errorTree = new Tree("");

                    foreach (var error in schemaErrors)
                    {
                        switch (error)
                        {
                            case ISchemaVersionChangeViolationError e:
                                errorTree.AddSchemaVersionChangeViolations(e);
                                break;
                            case IInvalidGraphQLSchemaError e:
                                errorTree.AddGraphQLSchemaErrors(e);
                                break;
                            case IPersistedQueryValidationError e:
                                errorTree.AddPersistedQueryValidationErrors(e);
                                break;
                            case IOpenApiCollectionValidationError e:
                                errorTree.AddOpenApiCollectionValidationErrors(e);
                                break;
                            case IMcpFeatureCollectionValidationError e:
                                errorTree.AddMcpFeatureCollectionValidationErrors(e);
                                break;
                            case IOperationsAreNotAllowedError e:
                                errorTree.AddErrorMessage(e.Message);
                                break;
                            case ISchemaVersionSyntaxError e:
                                errorTree.AddErrorMessage(e.Message);
                                break;
                            case IProcessingTimeoutError e:
                                errorTree.AddErrorMessage(e.Message);
                                break;
                            case IUnexpectedProcessingError e:
                                errorTree.AddErrorMessage(e.Message);
                                break;
                        }
                    }

                    activity.Fail(errorTree);

                    await activity.FailAllAsync();

                    console.Error.WriteErrorLine("Validation failed.");

                    return false;

                case ISchemaVersionValidationSuccess:
                    activity.Success("Validation passed.");

                    return true;

                case IOperationInProgress:
                case IValidationInProgress:
                    activity.Update("Validating...");
                    break;

                default:
                    activity.Update(ErrorMessages.UnknownServerResponse, ActivityUpdateKind.Warning);
                    break;
            }
        }

        return false;
    }
}
