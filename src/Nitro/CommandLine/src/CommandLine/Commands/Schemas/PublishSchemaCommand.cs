using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class PublishSchemaCommand : Command
{
    public PublishSchemaCommand(
        INitroConsole console,
        ISchemasClient client) : base("publish")
    {
        Description = "Publish a schema version to a stage";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<ForceOption>.Instance);
        Options.Add(Opt<OptionalWaitForApprovalOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                console,
                client,
                parseResult.GetValue(Opt<TagOption>.Instance)!,
                parseResult.GetValue(Opt<StageNameOption>.Instance)!,
                parseResult.GetValue(Opt<ApiIdOption>.Instance)!,
                parseResult.GetValue(Opt<ForceOption>.Instance),
                parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance),
                parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance),
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        ISchemasClient client,
        string tag,
        string stage,
        string apiId,
        bool force,
        bool waitForApproval,
        string? sourceMetadataJson,
        CancellationToken ct)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity("Publishing..."))
        {
            console.Log("Initialized");

            if (force)
            {
                console.Log("[yellow]Force push is enabled[/]");
            }

            console.Log("Create publish request");

            var publishRequest = await client.StartSchemaPublishAsync(
                apiId,
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

            console.Log($"Publish request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            await foreach (var update in client.SubscribeToSchemaPublishAsync(requestId, ct))
            {
                switch (update)
                {
                    case IProcessingTaskIsQueued v:
                        activity.Update(
                            $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                        break;

                    case ISchemaVersionPublishFailed { Errors: var schemaErrors }:
                        console.WriteLine("Schema publish failed");
                        console.PrintMutationErrors(schemaErrors);
                        return ExitCodes.Error;

                    case ISchemaVersionPublishSuccess:
                        console.Success("Successfully published schema!");
                        return ExitCodes.Success;

                    case IProcessingTaskIsReady:
                        console.Success("Your request is ready for the committing.");
                        break;

                    case IOperationInProgress:
                        activity.Update("The committing of your request is in progress.");
                        break;

                    case IWaitForApproval waitForApprovalEvent:
                        if (waitForApprovalEvent.Deployment is
                            IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Deployment_SchemaDeployment deployment)
                        {
                            console.PrintMutationErrors(deployment.Errors);
                        }

                        activity.Update(
                            "The committing of your request is waiting for approval. Check Nitro to approve the request.");
                        break;

                    case IProcessingTaskApproved:
                        activity.Update("The committing of your request is approved.");
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
