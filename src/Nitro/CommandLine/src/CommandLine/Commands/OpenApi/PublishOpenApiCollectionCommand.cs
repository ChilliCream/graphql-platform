using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class PublishOpenApiCollectionCommand : Command
{
    public PublishOpenApiCollectionCommand() : base("publish")
    {
        Description = "Publish an OpenAPI collection version to an stage";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<OpenApiCollectionIdOption>.Instance);
        AddOption(Opt<ForceOption>.Instance);
        AddOption(Opt<OptionalWaitForApprovalOption>.Instance);
        AddOption(Opt<OptionalSourceMetadataOption>.Instance);

        this.SetHandler(async context =>
        {
            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            var client = context.BindingContext.GetRequiredService<IOpenApiClient>();
            var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;
            var stage = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
            var openApiCollectionId = context.ParseResult.GetValueForOption(Opt<OpenApiCollectionIdOption>.Instance)!;
            var force = context.ParseResult.GetValueForOption(Opt<ForceOption>.Instance);
            var waitForApproval = context.ParseResult.GetValueForOption(Opt<OptionalWaitForApprovalOption>.Instance);
            var sourceMetadataJson = context.ParseResult.GetValueForOption(Opt<OptionalSourceMetadataOption>.Instance);

            context.ExitCode = await ExecuteAsync(
                console,
                client,
                tag,
                stage,
                openApiCollectionId,
                force,
                waitForApproval,
                sourceMetadataJson,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IOpenApiClient client,
        string tag,
        string stage,
        string openApiCollectionId,
        bool force,
        bool waitForApproval,
        string? sourceMetadataJson,
        CancellationToken ct)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity("Publishing..."))
        {
            if (force)
            {
                console.Log("[yellow]Force push is enabled[/]");
            }

            console.Log("Create publish request");

            var publishRequest = await client.StartOpenApiCollectionPublishAsync(
                openApiCollectionId,
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

            await foreach (var update in client.SubscribeToOpenApiCollectionPublishAsync(requestId, ct))
            {
                switch (update)
                {
                    case IProcessingTaskIsQueued v:
                        activity.Update(
                            $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                        break;

                    case IOpenApiCollectionVersionPublishFailed { Errors: var errors }:
                        console.WriteLine("OpenAPI collection publish failed");
                        console.PrintMutationErrors(errors);
                        return ExitCodes.Error;

                    case IOpenApiCollectionVersionPublishSuccess:
                        console.Success("Successfully published OpenAPI collection!");
                        return ExitCodes.Success;

                    case IProcessingTaskIsReady:
                        console.Success("Your request is ready for processing.");
                        break;

                    case IOperationInProgress:
                        activity.Update("Your request is in progress.");
                        break;

                    case IWaitForApproval waitForApprovalEvent:
                        if (waitForApprovalEvent.Deployment is
                            IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_Deployment_OpenApiCollectionDeployment deployment)
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
