using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
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
        Options.Add(Opt<ForceOption>.Instance);
        Options.Add(Opt<OptionalWaitForApprovalOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

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

        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var openApiCollectionId = parseResult.GetValue(Opt<OpenApiCollectionIdOption>.Instance)!;
        var force = parseResult.GetValue(Opt<ForceOption>.Instance);
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
                    child.FailAll();

                    foreach (var error in publishRequest.Errors)
                    {
                        var errorMessage = error switch
                        {
                            IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_UnauthorizedOperation err => err.Message,
                            IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_StageNotFoundError err => err.Message,
                            IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_OpenApiCollectionNotFoundError err => err.Message,
                            IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_OpenApiCollectionVersionNotFoundError err => err.Message,
                            IError err => "Unexpected mutation error: " + err.Message,
                            _ => "Unexpected mutation error."
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
                            child.FailAll();

                            foreach (var error in errors)
                            {
                                switch (error)
                                {
                                    case IUnexpectedProcessingError e:
                                        console.Error.WriteErrorLine(e.Message);
                                        break;
                                    case IProcessingTimeoutError e:
                                        console.Error.WriteErrorLine(e.Message);
                                        break;
                                    case IConcurrentOperationError e:
                                        console.Error.WriteErrorLine(e.Message);
                                        break;
                                    case IOpenApiCollectionValidationError e:
                                        console.PrintOpenApiCollectionValidationErrors(e);
                                        break;
                                    case IError e:
                                        console.Error.WriteErrorLine("Unexpected error: " + e.Message);
                                        break;
                                }
                            }

                            console.Error.WriteErrorLine("OpenAPI collection publish failed.");
                            return ExitCodes.Error;

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
                                foreach (var error in deployment.Errors)
                                {
                                    switch (error)
                                    {
                                        case IOpenApiCollectionValidationError e:
                                            console.PrintOpenApiCollectionValidationErrors(e);
                                            break;
                                        case IError e:
                                            console.Error.WriteErrorLine("Unexpected error: " + e.Message);
                                            break;
                                    }
                                }
                            }

                            child.Update("Waiting for approval. Approve in Nitro to continue.");
                            break;

                        case IProcessingTaskApproved:
                            child.Update("Approved. Processing...");
                            break;

                        default:
                            child.Update("Unknown server response. Consider updating the CLI.", ActivityUpdateKind.Warning);
                            break;
                    }
                }

                child.Fail();
            }
        }

        return ExitCodes.Error;
    }
}
