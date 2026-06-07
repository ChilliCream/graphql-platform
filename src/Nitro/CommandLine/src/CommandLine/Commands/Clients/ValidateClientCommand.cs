using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ValidateClientCommand : Command
{
    public ValidateClientCommand() : base("validate")
    {
        Description = "Validate a client version.";

        Options.Add(Opt<OptionalClientIdOption>.Instance);
        Options.Add(Opt<OptionalStageNameOption>.Instance);
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
        var apisClient = services.GetRequiredService<IApisClient>();
        var stagesClient = services.GetRequiredService<IStagesClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        string clientId;
        string? apiId = null;
        var clientIdArg = parseResult.GetValue(Opt<OptionalClientIdOption>.Instance);
        if (console.IsInteractive && clientIdArg is null)
        {
            apiId = await console.GetOrPromptForApiIdAsync(
                "For which API?", parseResult, apisClient, sessionService, ct);

            var selectedClient = await SelectClientPrompt
                .New(client, apiId)
                .Title("Select a client from the list below.")
                .RenderAsync(console, ct) ?? throw NoClientSelected();

            clientId = selectedClient.Id;
        }
        else
        {
            clientId = parseResult.GetRequiredOptionalValue(Opt<OptionalClientIdOption>.Instance);
        }

        var stageArg = parseResult.GetValue(Opt<OptionalStageNameOption>.Instance);
        if (string.IsNullOrEmpty(stageArg) && console.IsInteractive)
        {
            apiId ??= await client.GetClientApiIdAsync(clientId, ct)
                ?? throw new ExitException("The client was not found.");
        }

        var stage = await console.GetOrPromptForStageNameAsync(
            "Which stage?",
            parseResult,
            Opt<OptionalStageNameOption>.Instance,
            stagesClient,
            apiId ?? string.Empty,
            ct);

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

        await using (var activity = console.StartActivity(
            $"Validating client '{clientId.EscapeMarkup()}' against stage '{stage.EscapeMarkup()}'",
            "Failed to validate the client."))
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
                await activity.FailAllAsync();

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

            activity.Update($"Validation request created. {$"(ID: {id})".Dim()}");

            await foreach (var update in client.SubscribeToClientValidationAsync(id, ct))
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

                        await activity.FailAllAsync(errorTree, "Client failed validation.");

                        throw new ExitException("Client failed validation.");

                    case IClientVersionValidationSuccess:
                        activity.Success("Client passed validation.");

                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update(Messages.Validating);
                        break;

                    default:
                        activity.Update(Messages.UnknownServerResponse, ActivityUpdateKind.Warning);
                        break;
                }
            }

            await activity.FailAllAsync();
        }

        return ExitCodes.Error;
    }
}
