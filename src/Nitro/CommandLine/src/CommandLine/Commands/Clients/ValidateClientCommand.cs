using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ValidateClientCommand : Command
{
    public ValidateClientCommand(
        INitroConsole console,
        IClientsClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("validate")
    {
        Description = "Validate a client version";

        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ClientIdOption>.Instance);
        Options.Add(Opt<OperationsFileOption>.Instance);
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
        IClientsClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var clientId = parseResult.GetValue(Opt<ClientIdOption>.Instance)!;
        var operationsFilePath = parseResult.GetValue(Opt<OperationsFileOption>.Instance)!;
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity($"Validating client against stage '{stage.EscapeMarkup()}' of client '{clientId.EscapeMarkup()}'"))
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
                activity.Fail("Failed to validate the client.");

                foreach (var error in validationRequest.Errors)
                {
                    var errorMessage = error switch
                    {
                        IValidateClientVersion_ValidateClient_Errors_UnauthorizedOperation err => err.Message,
                        IValidateClientVersion_ValidateClient_Errors_ClientNotFoundError err => err.Message,
                        IValidateClientVersion_ValidateClient_Errors_StageNotFoundError err => err.Message,
                        IValidateClientVersion_ValidateClient_Errors_InvalidSourceMetadataInputError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (validationRequest.Id is not { } requestId)
            {
                throw new ExitException("Could not create client validation request.");
            }

            activity.Update($"Validation request created (ID: {requestId.EscapeMarkup()})");

            await foreach (var update in client.SubscribeToClientValidationAsync(requestId, ct))
            {
                switch (update)
                {
                    case IClientVersionValidationFailed { Errors: var errors }:
                        activity.Fail("Failed to validate the client.");

                        foreach (var error in errors)
                        {
                            switch (error)
                            {
                                case IPersistedQueryValidationError e:
                                    console.PrintPersistedQueryValidationErrors(e);
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

                        console.Error.WriteErrorLine("Client validation failed.");
                        return ExitCodes.Error;

                    case IClientVersionValidationSuccess:
                        activity.Success("Validated the client.");

                        resultHolder.SetResult(new ObjectResult(new ValidateClientResult
                        {
                            RequestId = requestId,
                            Status = "success"
                        }));

                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update("The client validation is in progress.");
                        break;

                    default:
                        activity.Update(
                            "Warning: Received an unknown server response. Ensure your CLI is on the latest version.");
                        break;
                }
            }

            activity.Fail("Failed to validate the client.");
        }

        return ExitCodes.Error;
    }

    public class ValidateClientResult
    {
        public required string RequestId { get; init; }

        public required string Status { get; init; }
    }
}
