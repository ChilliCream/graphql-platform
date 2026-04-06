using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class PublishOpenApiCollectionCommand : Command
{
    public PublishOpenApiCollectionCommand() : base("publish")
    {
        Description = "Publish an OpenAPI collection version to a stage.";

        Options.Add(Opt<OpenApiCollectionIdOption>.Instance);
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
            openapi publish \
              --openapi-collection-id "<collection-id>" \
              --stage "dev" \
              --tag "v1"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IOpenApiClient>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetRequiredValue(Opt<TagOption>.Instance);
        var stage = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var openApiCollectionId = parseResult.GetRequiredValue(Opt<OpenApiCollectionIdOption>.Instance);
        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
        var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var rootActivity = console.StartActivity(
            $"Publishing new OpenAPI collection version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'",
            "Failed to publish a new OpenAPI collection version."))
        {
            if (force)
            {
                rootActivity.Update("Force push is enabled.", ActivityUpdateKind.Warning);
            }

            string requestId;

            await using (var child = rootActivity.StartChildActivity(
                "Starting publish request",
                "Failed to start publish request."))
            {
                var publishRequest = await client.StartOpenApiCollectionPublishAsync(
                    openApiCollectionId,
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
                            IUnauthorizedOperation err => err.Message,
                            IInvalidSourceMetadataInputError err => err.Message,
                            IStageNotFoundError err => err.Message,
                            IOpenApiCollectionNotFoundError err => err.Message,
                            IOpenApiCollectionVersionNotFoundError err => err.Message,
                            IError err => ErrorMessages.UnexpectedMutationError(err),
                            _ => ErrorMessages.UnexpectedMutationError()
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
                "Processing",
                "Processing failed."))
            {
                await foreach (var update in client.SubscribeToOpenApiCollectionPublishAsync(requestId, ct))
                {
                    switch (update)
                    {
                        case IProcessingTaskIsQueued v:
                            child.Update($"Queued at position {v.QueuePosition}.");
                            break;

                        case IOpenApiCollectionVersionPublishFailed { Errors: var errors }:
                            var errorTree = new Tree("");

                            foreach (var error in errors)
                            {
                                switch (error)
                                {
                                    case IOpenApiCollectionValidationError e:
                                        errorTree.AddOpenApiCollectionValidationErrors(e);
                                        break;
                                    case IConcurrentOperationError e:
                                        errorTree.AddErrorMessage(e.Message);
                                        break;
                                    case IUnexpectedProcessingError e:
                                        errorTree.AddErrorMessage(e.Message);
                                        break;
                                    case IProcessingTimeoutError e:
                                        errorTree.AddErrorMessage(e.Message);
                                        break;
                                }
                            }

                            child.Fail(errorTree);

                            await child.FailAllAsync();

                            throw new ExitException("OpenAPI collection publish failed.");

                        case IOpenApiCollectionVersionPublishSuccess:
                            child.Success("Published successfully.");
                            rootActivity.Success($"Published new OpenAPI collection version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'.");
                            return ExitCodes.Success;

                        case IProcessingTaskIsReady:
                            child.Update("Ready.");
                            break;

                        case IOperationInProgress:
                            child.Update("Processing...");
                            break;

                        case IWaitForApproval waitForApprovalEvent:
                            if (waitForApprovalEvent.Deployment is IOpenApiCollectionDeployment deployment)
                            {
                                var approvalErrorTree = new Tree("");

                                foreach (var error in deployment.Errors)
                                {
                                    switch (error)
                                    {
                                        case IOpenApiCollectionValidationError e:
                                            approvalErrorTree.AddOpenApiCollectionValidationErrors(e);
                                            break;
                                    }
                                }

                                child.Fail(approvalErrorTree);
                            }

                            child.Update("Waiting for approval. Approve in Nitro to continue.", ActivityUpdateKind.Waiting);
                            break;

                        case IProcessingTaskApproved:
                            child.Update("Approved. Processing...");
                            break;

                        default:
                            child.Update(ErrorMessages.UnknownServerResponse, ActivityUpdateKind.Warning);
                            break;
                    }
                }

                child.Fail();
            }
        }

        return ExitCodes.Error;
    }
}
