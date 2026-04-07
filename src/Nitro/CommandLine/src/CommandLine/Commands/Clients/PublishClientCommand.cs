using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class PublishClientCommand : Command
{
    public PublishClientCommand() : base("publish")
    {
        Description = "Publish a client version to a stage.";

        Options.Add(Opt<ClientIdOption>.Instance);
        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
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
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetRequiredValue(Opt<TagOption>.Instance);
        var stage = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var clientId = parseResult.GetRequiredValue(Opt<ClientIdOption>.Instance);
        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
        var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var rootActivity = console.StartActivity(
            $"Publishing new client version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}' of client '{clientId.EscapeMarkup()}'",
            "Failed to publish a new client version."))
        {
            if (force)
            {
                rootActivity.Update(Messages.ForcePushEnabled, ActivityUpdateKind.Warning);
            }

            string requestId;

            await using (var child = rootActivity.StartChildActivity(
                Messages.StartingPublishRequest,
                Messages.FailedToStartPublishRequest))
            {
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
                    await child.FailAllAsync();

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

                requestId = id;
                child.Success($"Publish request created (ID: {requestId.EscapeMarkup()}).");
            }

            await using (var child = rootActivity.StartChildActivity(
                Messages.ProcessingActivity,
                Messages.ProcessingFailed))
            {
                await foreach (var update in client.SubscribeToClientPublishAsync(requestId, ct))
                {
                    switch (update)
                    {
                        case IProcessingTaskIsQueued v:
                            child.Update(
                                $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
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

                            child.Fail(errorTree);

                            await child.FailAllAsync();

                            throw new ExitException("Client publish failed.");

                        case IClientVersionPublishSuccess:
                            child.Success(Messages.PublishedSuccessfully);
                            rootActivity.Success($"Published new client version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'.");

                            return ExitCodes.Success;

                        case IProcessingTaskIsReady:
                            child.Update(Messages.RequestReadyForProcessing);
                            break;

                        case IOperationInProgress:
                            child.Update(Messages.RequestBeingProcessed);
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

                                child.Fail(approvalErrorTree);
                            }

                            child.Update(
                                Messages.WaitingForApproval, ActivityUpdateKind.Waiting);
                            break;

                        case IProcessingTaskApproved:
                            child.Update(Messages.RequestApproved);
                            break;

                        default:
                            child.Update(
                                Messages.UnknownServerResponse, ActivityUpdateKind.Warning);
                            break;
                    }
                }

                child.Fail();
            }
        }

        return ExitCodes.Error;
    }
}
