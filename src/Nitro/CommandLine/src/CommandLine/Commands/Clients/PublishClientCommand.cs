using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class PublishClientCommand : Command
{
    public PublishClientCommand() : base("publish")
    {
        Description = "Publish a client version to a stage.";

        Options.Add(Opt<OptionalClientIdOption>.Instance);
        Options.Add(Opt<OptionalTagOption>.Instance);
        Options.Add(Opt<OptionalStageNameOption>.Instance);
        Options.Add(Opt<OptionalForceOption>.Instance);
        Options.Add(Opt<OptionalWaitForApprovalOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        Validators.Add(result =>
        {
            var forceResult = result.GetResult(Opt<OptionalForceOption>.Instance);
            var waitResult = result.GetResult(Opt<OptionalWaitForApprovalOption>.Instance);

            if (forceResult is { Implicit: false } && waitResult is { Implicit: false })
            {
                result.AddError(
                    "The '--force' and '--wait-for-approval' options are mutually exclusive.");
            }
        });

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            client publish \
              --client-id "<client-id>" \
              --tag "v1" \
              --stage "dev"
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

        var tag = await console.GetOrPromptForTagAsync(
            "Which tag?", parseResult, Opt<OptionalTagOption>.Instance, ct);

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

        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
        var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity(
            $"Publishing new version '{tag.EscapeMarkup()}' of client '{clientId.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'",
            "Failed to publish a new client version."))
        {
            if (force)
            {
                activity.Update(Messages.ForcePushEnabled, ActivityUpdateKind.Warning);
            }

            var publishRequest = await client.StartClientPublishAsync(
                clientId,
                stage,
                tag,
                force,
                waitForApproval,
                source,
                ct);

            if (publishRequest.Errors?.Count > 0)
            {
                await activity.FailAllAsync();

                foreach (var error in publishRequest.Errors)
                {
                    var errorMessage = error switch
                    {
                        IPublishClientVersion_PublishClient_Errors_UnauthorizedOperation err => err.Message,
                        IPublishClientVersion_PublishClient_Errors_ClientNotFoundError err => err.Message,
                        IPublishClientVersion_PublishClient_Errors_StageNotFoundError err => err.Message,
                        IPublishClientVersion_PublishClient_Errors_ClientVersionNotFoundError err => err.Message,
                        IPublishClientVersion_PublishClient_Errors_InvalidSourceMetadataInputError err => err.Message,
                        IError err => Messages.UnexpectedMutationError(err),
                        _ => Messages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (publishRequest.Id is not { } id)
            {
                throw MutationReturnedNoData();
            }

            activity.Update($"Publication request created. {$"(ID: {id})".Dim()}");

            await foreach (var update in client.SubscribeToClientPublishAsync(id, ct))
            {
                switch (update)
                {
                    case IProcessingTaskIsQueued v:
                        activity.Update(Messages.QueuedAtPosition(v.QueuePosition), ActivityUpdateKind.Waiting);
                        break;

                    case IClientVersionPublishFailed { Errors: var errors }:
                        var errorTree = new Tree("");

                        foreach (var error in errors)
                        {
                            switch (error)
                            {
                                case IPersistedQueryValidationError e:
                                    errorTree.AddPersistedQueryValidationErrors(e);
                                    break;
                                case IConcurrentOperationError e:
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

                        await activity.FailAllAsync(errorTree, "Client version was rejected.");

                        throw new ExitException("Client version was rejected.");

                    case IClientVersionPublishSuccess:
                        activity.Success($"Published new client version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'.");

                        return ExitCodes.Success;

                    case IProcessingTaskIsReady:
                        activity.Update(Messages.RequestReadyForProcessing);
                        break;

                    case IOperationInProgress:
                        activity.Update(Messages.RequestBeingProcessed);
                        break;

                    case IWaitForApproval waitForApprovalEvent:
                        if (waitForApprovalEvent.Deployment is IClientDeployment deployment)
                        {
                            var approvalErrorTree = new Tree("");

                            foreach (var error in deployment.Errors)
                            {
                                switch (error)
                                {
                                    case IPersistedQueryValidationError e:
                                        approvalErrorTree.AddPersistedQueryValidationErrors(e);
                                        break;
                                }
                            }

                            activity.Update(
                                Messages.ValidationFailed,
                                ActivityUpdateKind.Warning,
                                approvalErrorTree);
                        }

                        activity.Update(
                            Messages.WaitingForApproval,
                            ActivityUpdateKind.Waiting);
                        break;

                    case IProcessingTaskApproved:
                        activity.Update(Messages.RequestApproved);
                        break;

                    default:
                        activity.Update(
                            Messages.UnknownServerResponse, ActivityUpdateKind.Warning);
                        break;
                }
            }

            await activity.FailAllAsync();
        }

        return ExitCodes.Error;
    }
}
