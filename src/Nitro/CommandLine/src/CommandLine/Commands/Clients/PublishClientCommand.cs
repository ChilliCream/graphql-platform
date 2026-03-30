using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class PublishClientCommand : Command
{
    public PublishClientCommand(
        INitroConsole console,
        IClientsClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("publish")
    {
        Description = "Publish a client version to a stage";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ClientIdOption>.Instance);
        Options.Add(Opt<ForceOption>.Instance);
        Options.Add(Opt<OptionalWaitForApprovalOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                parseResult,
                console,
                client,
                sessionService,
                resultHolder,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IClientsClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var clientId = parseResult.GetValue(Opt<ClientIdOption>.Instance)!;
        var force = parseResult.GetValue(Opt<ForceOption>.Instance);
        var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity($"Publishing new client version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}' of client '{clientId.EscapeMarkup()}'"))
        {
            if (force)
            {
                activity.Warning("Force push is enabled.");
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
                activity.Fail("Failed to publish a new client version.");

                foreach (var error in publishRequest.Errors)
                {
                    var errorMessage = error switch
                    {
                        IPublishClientVersion_PublishClient_Errors_UnauthorizedOperation err => err.Message,
                        IPublishClientVersion_PublishClient_Errors_ClientNotFoundError err => err.Message,
                        IPublishClientVersion_PublishClient_Errors_StageNotFoundError err => err.Message,
                        IPublishClientVersion_PublishClient_Errors_ClientVersionNotFoundError err => err.Message,
                        IPublishClientVersion_PublishClient_Errors_InvalidSourceMetadataInputError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (publishRequest.Id is not { } requestId)
            {
                activity.Fail("Failed to publish a new client version.");
                console.Error.WriteErrorLine("Could not create publish request.");
                return ExitCodes.Error;
            }

            await foreach (var update in client.SubscribeToClientPublishAsync(requestId, ct))
            {
                switch (update)
                {
                    case IProcessingTaskIsQueued v:
                        activity.Update(
                            $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                        break;

                    case IClientVersionPublishFailed { Errors: var errors }:
                        activity.Fail("Failed to publish a new client version.");

                        foreach (var error in errors)
                        {
                            switch (error)
                            {
                                case IConcurrentOperationError e:
                                    console.Error.WriteErrorLine(e.Message);
                                    break;
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

                        console.Error.WriteErrorLine("Client publish failed.");
                        return ExitCodes.Error;

                    case IClientVersionPublishSuccess:
                        activity.Success($"Published new client version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'.");

                        resultHolder.SetResult(new ObjectResult(new PublishClientResult
                        {
                            Stage = stage,
                            Status = "success"
                        }));

                        return ExitCodes.Success;

                    case IProcessingTaskIsReady:
                        console.Success("Your request is ready for the committing.");
                        break;

                    case IOperationInProgress:
                        activity.Update("The committing of your request is in progress.");
                        break;

                    case IWaitForApproval waitForApprovalEvent:
                        if (waitForApprovalEvent.Deployment is
                            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_ClientDeployment deployment)
                        {
                            foreach (var error in deployment.Errors)
                            {
                                switch (error)
                                {
                                    case IPersistedQueryValidationError e:
                                        console.PrintPersistedQueryValidationErrors(e);
                                        break;
                                    case IError e:
                                        console.Error.WriteErrorLine("Unexpected error: " + e.Message);
                                        break;
                                }
                            }
                        }

                        activity.Update(
                            "The committing of your request is waiting for approval. Check Nitro to approve the request.");
                        break;

                    case IProcessingTaskApproved:
                        activity.Update("The committing of your request is approved.");
                        break;

                    default:
                        activity.Update(
                            "Warning: Received an unknown server response. Ensure your CLI is on the latest version.");
                        break;
                }
            }

            activity.Fail("Failed to publish a new client version.");
        }

        return ExitCodes.Error;
    }

    public class PublishClientResult
    {
        public required string Stage { get; init; }

        public required string Status { get; init; }
    }
}
