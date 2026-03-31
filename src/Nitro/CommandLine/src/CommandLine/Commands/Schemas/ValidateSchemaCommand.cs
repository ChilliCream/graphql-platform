using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class ValidateSchemaCommand : Command
{
    public ValidateSchemaCommand(
        INitroConsole console,
        ISchemasClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("validate")
    {
        Description = "Validates a schema against a stage";

        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<SchemaFileOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                parseResult,
                console,
                client,
                fileSystem,
                sessionService,
                resultHolder,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        ISchemasClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
        var schemaFilePath = parseResult.GetValue(Opt<SchemaFileOption>.Instance)!;
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity(
            $"Validating schema against stage '{stage.EscapeMarkup()}' of API '{apiId.EscapeMarkup()}'",
            "Failed to validate the schema."))
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
                activity.Fail();

                foreach (var error in validationRequest.Errors)
                {
                    var errorMessage = error switch
                    {
                        IValidateSchemaVersion_ValidateSchema_Errors_UnauthorizedOperation err => err.Message,
                        IValidateSchemaVersion_ValidateSchema_Errors_ApiNotFoundError err => err.Message,
                        IValidateSchemaVersion_ValidateSchema_Errors_StageNotFoundError err => err.Message,
                        IValidateSchemaVersion_ValidateSchema_Errors_SchemaNotFoundError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (validationRequest.Id is not { } requestId)
            {
                throw MutationReturnedNoData();
            }

            activity.Update($"Validation request created (ID: {requestId.EscapeMarkup()})");

            await foreach (var update in client.SubscribeToSchemaValidationAsync(requestId, ct))
            {
                switch (update)
                {
                    case ISchemaVersionValidationFailed { Errors: var schemaErrors }:
                        activity.Fail();

                        foreach (var error in schemaErrors)
                        {
                            switch (error)
                            {
                                case ISchemaVersionChangeViolationError e:
                                    console.PrintSchemaVersionChangeViolations(e);
                                    break;
                                case ISchemaChangeViolationError e:
                                    console.PrintSchemaChangeViolations(e);
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
                                case IError e:
                                    console.Error.WriteErrorLine("Unexpected error: " + e.Message);
                                    break;
                            }
                        }

                        console.Error.WriteErrorLine("Schema validation failed.");
                        return ExitCodes.Error;

                    case ISchemaVersionValidationSuccess:
                        activity.Success($"Validated schema against stage '{stage.EscapeMarkup()}'.");

                        if (!console.IsHumanReadable)
                        {
                            resultHolder.SetResult(new ObjectResult(new ValidateSchemaResult
                            {
                                RequestId = requestId,
                                Status = "success"
                            }));
                        }

                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update("The schema validation is in progress.");
                        break;

                    default:
                        activity.Update(
                            "Warning: Received an unknown server response. Ensure your CLI is on the latest version.");
                        break;
                }
            }

            activity.Fail();
        }

        return ExitCodes.Error;
    }

    public class ValidateSchemaResult
    {
        public required string RequestId { get; init; }

        public required string Status { get; init; }
    }
}
