using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class PublishSchemaCommand : Command
{
    public PublishSchemaCommand(
        INitroConsole console,
        ISchemasClient client,
        ISessionService sessionService) : base("publish")
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
                parseResult,
                console,
                client,
                sessionService,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        ISchemasClient client,
        ISessionService sessionService,
        CancellationToken ct)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
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
                activity.Fail();

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

            // console.Log($"Publish request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            await foreach (var update in client.SubscribeToSchemaPublishAsync(requestId, ct))
            {
                switch (update)
                {
                    case IProcessingTaskIsQueued v:
                        activity.Update(
                            $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                        break;

                    case ISchemaVersionPublishFailed { Errors: var schemaErrors }:
                        activity.Fail();

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
                                    await console.Error.WriteLineAsync(e.Message);
                                    break;
                                case IOperationsAreNotAllowedError e:
                                    await console.Error.WriteLineAsync(e.Message);
                                    break;
                                case ISchemaVersionSyntaxError e:
                                    await console.Error.WriteLineAsync(e.Message);
                                    break;
                                case IProcessingTimeoutError e:
                                    await console.Error.WriteLineAsync(e.Message);
                                    break;
                                case IUnexpectedProcessingError e:
                                    await console.Error.WriteLineAsync(e.Message);
                                    break;
                                case IError e:
                                    await console.Error.WriteLineAsync("Unexpected error: " + e.Message);
                                    break;
                            }
                        }

                        await console.Error.WriteLineAsync("Schema publish failed.");
                        return ExitCodes.Error;

                    case ISchemaVersionPublishSuccess:
                        activity.Success("Successfully published schema!");
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
                                        await console.Error.WriteLineAsync(e.Message);
                                        break;
                                    case ISchemaVersionSyntaxError e:
                                        await console.Error.WriteLineAsync(e.Message);
                                        break;
                                    case IError e:
                                        await console.Error.WriteLineAsync("Unexpected error: " + e.Message);
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

            activity.Fail();
        }

        return ExitCodes.Error;
    }
}
