using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class PublishSchemaCommand : Command
{
    public PublishSchemaCommand() : base("publish")
    {
        Description = "Publish a schema version to a stage.";

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ForceOption>.Instance);
        Options.Add(Opt<OptionalWaitForApprovalOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<ISchemasClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
        var force = parseResult.GetValue(Opt<ForceOption>.Instance);
        var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var rootActivity = console.StartActivity(
            $"Publishing new schema version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}' of API '{apiId.EscapeMarkup()}'",
            "Failed to publish a new schema version."))
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
                var publishRequest = await client.StartSchemaPublishAsync(
                    apiId,
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
                            IPublishSchemaVersion_PublishSchema_Errors_UnauthorizedOperation err => err.Message,
                            IPublishSchemaVersion_PublishSchema_Errors_ApiNotFoundError err => err.Message,
                            IPublishSchemaVersion_PublishSchema_Errors_StageNotFoundError err => err.Message,
                            IPublishSchemaVersion_PublishSchema_Errors_SchemaNotFoundError err => err.Message,
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
                await foreach (var update in client.SubscribeToSchemaPublishAsync(requestId, ct))
                {
                    switch (update)
                    {
                        case IProcessingTaskIsQueued v:
                            child.Update(
                                $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                            break;

                        case ISchemaVersionPublishFailed { Errors: var schemaErrors }:
                            child.FailAll();

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
                                    case IConcurrentOperationError e:
                                        console.Error.WriteErrorLine(e.Message);
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

                            console.Error.WriteErrorLine("Schema publish failed.");
                            return ExitCodes.Error;

                        case ISchemaVersionPublishSuccess:
                            child.Success("Published successfully.");
                            rootActivity.Success($"Published new schema version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'.");

                            if (!console.IsHumanReadable)
                            {
                                resultHolder.SetResult(new ObjectResult(new PublishSchemaResult
                                {
                                    Stage = stage,
                                    Status = "success"
                                }));
                            }

                            return ExitCodes.Success;

                        case IProcessingTaskIsReady:
                            child.Update("Your request is ready for processing.");
                            break;

                        case IOperationInProgress:
                            child.Update("Your request is being processed.");
                            break;

                        case IWaitForApproval waitForApprovalEvent:
                            if (waitForApprovalEvent.Deployment is
                                IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Deployment_SchemaDeployment deployment)
                            {
                                foreach (var error in deployment.Errors)
                                {
                                    switch (error)
                                    {
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
                                        case IError e:
                                            console.Error.WriteErrorLine("Unexpected error: " + e.Message);
                                            break;
                                    }
                                }
                            }

                            child.Update(
                                "Your request is waiting for approval. Check Nitro to approve the request.");
                            break;

                        case IProcessingTaskApproved:
                            child.Update("Your request has been approved.");
                            break;

                        default:
                            child.Update(
                                "Unknown server response. Ensure your CLI is on the latest version.", ActivityUpdateKind.Warning);
                            break;
                    }
                }

                child.Fail();
            }
        }

        return ExitCodes.Error;
    }

    public class PublishSchemaResult
    {
        public required string Stage { get; init; }

        public required string Status { get; init; }
    }
}
