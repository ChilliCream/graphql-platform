using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class PublishOpenApiCollectionCommand : Command
{
    public PublishOpenApiCollectionCommand(
        INitroConsole console,
        IOpenApiClient client) : base("publish")
    {
        Description = "Publish an OpenAPI collection version to a stage";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<OpenApiCollectionIdOption>.Instance);
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
                parseResult.GetValue(Opt<OpenApiCollectionIdOption>.Instance)!,
                parseResult.GetValue(Opt<ForceOption>.Instance),
                parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance),
                parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance),
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
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
                activity.Fail();

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

                    await console.Error.WriteLineAsync(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (publishRequest.Id is not { } requestId)
            {
                activity.Fail();
                await console.Error.WriteLineAsync("Could not create publish request.");
                return ExitCodes.Error;
            }

            await foreach (var update in client.SubscribeToOpenApiCollectionPublishAsync(requestId, ct))
            {
                switch (update)
                {
                    case IProcessingTaskIsQueued v:
                        activity.Update(
                            $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                        break;

                    case IOpenApiCollectionVersionPublishFailed { Errors: var errors }:
                        activity.Fail();

                        foreach (var error in errors)
                        {
                            switch (error)
                            {
                                case IUnexpectedProcessingError e:
                                    await console.Error.WriteLineAsync(e.Message);
                                    break;
                                case IProcessingTimeoutError e:
                                    await console.Error.WriteLineAsync(e.Message);
                                    break;
                                case IConcurrentOperationError e:
                                    await console.Error.WriteLineAsync(e.Message);
                                    break;
                                case IOpenApiCollectionValidationError e:
                                    console.PrintOpenApiCollectionValidationErrors(e);
                                    break;
                                case IError e:
                                    await console.Error.WriteLineAsync("Unexpected error: " + e.Message);
                                    break;
                            }
                        }

                        await console.Error.WriteLineAsync("OpenAPI collection publish failed.");
                        return ExitCodes.Error;

                    case IOpenApiCollectionVersionPublishSuccess:
                        activity.Success("Successfully published OpenAPI collection!");
                        return ExitCodes.Success;

                    case IProcessingTaskIsReady:
                        console.Success("Your request is ready for processing.");
                        break;

                    case IOperationInProgress:
                        activity.Update("Your request is in progress.");
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
                                        await console.Error.WriteLineAsync("Unexpected error: " + e.Message);
                                        break;
                                }
                            }
                        }

                        activity.Update(
                            "The processing of your request is waiting for approval. Check Nitro to approve the request.");
                        break;

                    case IProcessingTaskApproved:
                        activity.Update("The processing of your request is approved.");
                        break;

                    default:
                        activity.Update(
                            "Warning: Received an unknown server response. Ensure your CLI is on the latest version.");
                        break;
                }
            }

            activity.Fail();
        }

        return ExitCodes.Error;
    }
}
