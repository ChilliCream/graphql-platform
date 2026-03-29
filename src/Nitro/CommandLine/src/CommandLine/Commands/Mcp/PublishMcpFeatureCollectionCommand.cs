using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class PublishMcpFeatureCollectionCommand : Command
{
    public PublishMcpFeatureCollectionCommand(
        INitroConsole console,
        IMcpClient client) : base("publish")
    {
        Description = "Publish an MCP Feature Collection version to a stage";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<McpFeatureCollectionIdOption>.Instance);
        Options.Add(Opt<ForceOption>.Instance);
        Options.Add(Opt<OptionalWaitForApprovalOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(
            console,
            async (parseResult, cancellationToken)
                => await ExecuteAsync(parseResult, console, client, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IMcpClient client,
        CancellationToken ct)
    {
        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var mcpFeatureCollectionId = parseResult.GetValue(Opt<McpFeatureCollectionIdOption>.Instance)!;
        var force = parseResult.GetValue(Opt<ForceOption>.Instance);
        var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity("Publishing..."))
        {
            if (force)
            {
                console.Log("[yellow]Force push is enabled[/]");
            }

            var publishRequest = await client.StartMcpFeatureCollectionPublishAsync(
                mcpFeatureCollectionId,
                stage,
                tag,
                force,
                waitForApproval,
                source,
                ct);

            console.PrintMutationErrorsAndExit(publishRequest.Errors);
            if (publishRequest.Id is not { } requestId)
            {
                throw new ExitException("Could not create publish request!");
            }

            // console.Log($"Publish request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            await foreach (var update in client.SubscribeToMcpFeatureCollectionPublishAsync(requestId, ct))
            {
                switch (update)
                {
                    case IProcessingTaskIsQueued v:
                        activity.Update(
                            $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                        break;

                    case IMcpFeatureCollectionVersionPublishFailed { Errors: var errors }:
                        console.ErrorLine("MCP Feature Collection publish failed");
                        console.PrintMutationErrors(errors);
                        return ExitCodes.Error;

                    case IMcpFeatureCollectionVersionPublishSuccess:
                        console.Success("Successfully published MCP Feature Collection!");
                        return ExitCodes.Success;

                    case IProcessingTaskIsReady:
                        console.Success("Your request is ready for processing.");
                        break;

                    case IOperationInProgress:
                        activity.Update("Your request is in progress.");
                        break;

                    case IWaitForApproval waitForApprovalEvent:
                        if (waitForApprovalEvent.Deployment is IMcpFeatureCollectionDeployment deployment)
                        {
                            console.PrintMutationErrors(deployment.Errors);
                        }

                        activity.Update(
                            "The processing of your request is waiting for approval. Check Nitro to approve the request.");
                        break;

                    case IProcessingTaskApproved:
                        activity.Update("The processing of your request is approved.");
                        break;

                    default:
                        activity.Update(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }

        return ExitCodes.Error;
    }
}
