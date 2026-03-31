using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class PublishMcpFeatureCollectionCommand : Command
{
    public PublishMcpFeatureCollectionCommand() : base("publish")
    {
        Description = "Publish an MCP feature collection version to a stage.";

        Options.Add(Opt<McpFeatureCollectionIdOption>.Instance);
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
        var client = services.GetRequiredService<IMcpClient>();

        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var mcpFeatureCollectionId = parseResult.GetValue(Opt<McpFeatureCollectionIdOption>.Instance)!;
        var force = parseResult.GetValue(Opt<ForceOption>.Instance);
        var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity(
            $"Publishing new MCP feature collection version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'",
            "Failed to publish a new MCP feature collection version."))
        {
            if (force)
            {
                activity.Warning("Force push is enabled.");
            }

            var publishRequest = await client.StartMcpFeatureCollectionPublishAsync(
                mcpFeatureCollectionId,
                stage,
                tag,
                force,
                waitForApproval,
                source,
                ct);

            if (publishRequest.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in publishRequest.Errors)
                {
                    var errorMessage = error switch
                    {
                        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_UnauthorizedOperation err => err.Message,
                        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_StageNotFoundError err => err.Message,
                        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError err => err.Message,
                        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_McpFeatureCollectionVersionNotFoundError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (publishRequest.Id is not { } requestId)
            {
                throw MutationReturnedNoData();
            }

            await foreach (var update in client.SubscribeToMcpFeatureCollectionPublishAsync(requestId, ct))
            {
                switch (update)
                {
                    case IProcessingTaskIsQueued v:
                        activity.Update($"Queued at position {v.QueuePosition}.");
                        break;

                    case IMcpFeatureCollectionVersionPublishFailed { Errors: var errors }:
                        activity.Fail();

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
                                case IMcpFeatureCollectionValidationError e:
                                    console.PrintMcpFeatureCollectionValidationErrors(e);
                                    break;
                                case IError e:
                                    console.Error.WriteErrorLine("Unexpected error: " + e.Message);
                                    break;
                            }
                        }

                        console.Error.WriteErrorLine("MCP Feature Collection publish failed.");
                        return ExitCodes.Error;

                    case IMcpFeatureCollectionVersionPublishSuccess:
                        activity.Success($"Published new MCP feature collection version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'.");
                        return ExitCodes.Success;

                    case IProcessingTaskIsReady:
                        activity.Update("Ready.");
                        break;

                    case IOperationInProgress:
                        activity.Update("Processing...");
                        break;

                    case IWaitForApproval waitForApprovalEvent:
                        if (waitForApprovalEvent.Deployment is IMcpFeatureCollectionDeployment deployment)
                        {
                            foreach (var error in deployment.Errors)
                            {
                                switch (error)
                                {
                                    case IMcpFeatureCollectionValidationError e:
                                        console.PrintMcpFeatureCollectionValidationErrors(e);
                                        break;
                                    case IError e:
                                        console.Error.WriteErrorLine("Unexpected error: " + e.Message);
                                        break;
                                }
                            }
                        }

                        activity.Update("Waiting for approval. Approve in Nitro to continue.");
                        break;

                    case IProcessingTaskApproved:
                        activity.Update("Approved. Processing...");
                        break;

                    default:
                        activity.Warning("Unknown server response. Consider updating the CLI.");
                        break;
                }
            }

            activity.Fail();
        }

        return ExitCodes.Error;
    }
}
