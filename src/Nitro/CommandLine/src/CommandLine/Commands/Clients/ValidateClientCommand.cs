using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ValidateClientCommand : Command
{
    public ValidateClientCommand() : base("validate")
    {
        Description = "Validate a client version.";

        Options.Add(Opt<ClientIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<OperationsFileOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            client validate \
              --client-id "<client-id>" \
              --stage "dev" \
              --operations-file ./operations.json
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IClientsClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var stage = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var clientId = parseResult.GetRequiredValue(Opt<ClientIdOption>.Instance);
        var operationsFilePath = parseResult.GetRequiredValue(Opt<OperationsFileOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        if (!Path.IsPathRooted(operationsFilePath))
        {
            operationsFilePath = Path.Combine(fileSystem.GetCurrentDirectory(), operationsFilePath);
        }

        if (!fileSystem.FileExists(operationsFilePath))
        {
            throw new ExitException(Messages.OperationsFileDoesNotExist(operationsFilePath));
        }

        await using (var rootActivity = console.StartActivity(
            $"Validating client '{clientId.EscapeMarkup()}' against stage '{stage.EscapeMarkup()}'",
            "Failed to validate the client."))
        {
            string requestId;

            await using (var child = rootActivity.StartChildActivity(
                Messages.StartingValidationRequest,
                Messages.FailedToStartValidationRequest))
            {
                await using var stream = fileSystem.OpenReadStream(operationsFilePath);

                var validationRequest = await client.StartClientValidationAsync(
                    clientId,
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
                            IValidateClientVersion_ValidateClient_Errors_UnauthorizedOperation err => err.Message,
                            IValidateClientVersion_ValidateClient_Errors_ClientNotFoundError err => err.Message,
                            IValidateClientVersion_ValidateClient_Errors_StageNotFoundError err => err.Message,
                            IValidateClientVersion_ValidateClient_Errors_InvalidSourceMetadataInputError err => err.Message,
                            IError err => Messages.UnexpectedMutationError(err),
                            _ => Messages.UnexpectedMutationError()
                        };

                        console.Error.WriteErrorLine(errorMessage);
                    }

                    return ExitCodes.Error;
                }

                if (validationRequest.Id is not { } id)
                {
                    throw new ExitException("Could not create client validation request.");
                }

                requestId = id;
                child.Success($"Validation request created (ID: {requestId.EscapeMarkup()}).");
            }

            await using (var child = rootActivity.StartChildActivity(
                Messages.ValidatingActivity,
                Messages.ValidationFailed))
            {
                await foreach (var update in client.SubscribeToClientValidationAsync(requestId, ct))
                {
                    switch (update)
                    {
                        case IClientVersionValidationFailed { Errors: var errors }:
                            var errorTree = new Tree("");

                            foreach (var error in errors)
                            {
                                switch (error)
                                {
                                    case IPersistedQueryValidationError e:
                                        errorTree.AddPersistedQueryValidationErrors(e);
                                        break;
                                    case IProcessingTimeoutError e:
                                        errorTree.AddErrorMessage(e.Message);
                                        break;
                                    case IUnexpectedProcessingError e:
                                        errorTree.AddErrorMessage(e.Message);
                                        break;
                                }
                            }

                            child.Fail(errorTree);

                            await child.FailAllAsync();

                            throw new ExitException("Client validation failed.");

                        case IClientVersionValidationSuccess:
                            child.Success(Messages.ValidationPassed);
                            rootActivity.Success($"Validated client against stage '{stage.EscapeMarkup()}'.");

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
