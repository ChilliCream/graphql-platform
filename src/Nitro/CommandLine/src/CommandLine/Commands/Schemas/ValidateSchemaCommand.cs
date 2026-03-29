using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class ValidateSchemaCommand : Command
{
    public ValidateSchemaCommand(
        INitroConsole console,
        ISchemasClient client,
        IFileSystem fileSystem,
        ISessionService sessionService) : base("validate")
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
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        ISchemasClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        CancellationToken ct)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
        var schemaFilePath = parseResult.GetValue(Opt<SchemaFileOption>.Instance)!;
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity("Validating schema..."))
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

                    await console.Error.WriteLineAsync(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (validationRequest.Id is not { } requestId)
            {
                throw new ExitException("Could not create schema validation request.");
            }

            activity.Update($"Validation request created (ID: {requestId.EscapeMarkup()})");

            await foreach (var update in client.SubscribeToSchemaValidationAsync(requestId, ct))
            {
                switch (update)
                {
                    case ISchemaVersionValidationFailed { Errors: var schemaErrors }:
                        activity.Fail();
                        // TODO: This should be more explicit
                        console.PrintMutationErrors(schemaErrors);

                        // TODO: Also output as result.
                        return ExitCodes.Error;

                    case ISchemaVersionValidationSuccess:
                        activity.Success("Schema validation succeeded.");
                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update("The schema validation is in progress.");
                        break;

                    default:
                        // TODO: Pull this out into a error messages class so other commands can use it.
                        activity.Update(
                            "Warning: Received a unknown server response. Ensure your CLI is on the latest version.");
                        break;
                }
            }

            activity.Fail();
        }

        return ExitCodes.Error;
    }
}
