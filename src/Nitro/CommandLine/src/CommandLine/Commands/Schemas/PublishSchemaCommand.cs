using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;
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
            schema publish \
              --api-id "<api-id>" \
              --tag "v1" \
              --stage "dev"
            """);

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

        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetRequiredValue(Opt<TagOption>.Instance);
        var stage = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var apiId = parseResult.GetRequiredValue(Opt<ApiIdOption>.Instance);
        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
        var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity(
            $"Publishing new schema version '{tag.EscapeMarkup()}' of API '{apiId.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'",
            "Failed to publish new schema version."))
        {
            if (force)
            {
                activity.Update(Messages.ForcePushEnabled, ActivityUpdateKind.Warning);
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
                await activity.FailAllAsync();

                foreach (var error in publishRequest.Errors)
                {
                    var errorMessage = error switch
                    {
                        IUnauthorizedOperation err => err.Message,
                        IInvalidSourceMetadataInputError err => err.Message,
                        IApiNotFoundError err => err.Message,
                        IStageNotFoundError err => err.Message,
                        ISchemaNotFoundError err => err.Message,
                        IError err => Messages.UnexpectedMutationError(err),
                        _ => Messages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (publishRequest.Id is not { } id)
            {
                throw MutationReturnedNoData();
            }

            activity.Update($"Publication request created. {$"(ID: {id})".Dim()}");

            await foreach (var update in client.SubscribeToSchemaPublishAsync(id, ct))
            {
                switch (update)
                {
                    case IProcessingTaskIsQueued v:
                        activity.Update(Messages.QueuedAtPosition(v.QueuePosition), ActivityUpdateKind.Waiting);
                        break;

                    case ISchemaVersionPublishFailed { Errors: var schemaErrors }:
                        var errorTree = new Tree("");

                        foreach (var error in schemaErrors)
                        {
                            switch (error)
                            {
                                case ISchemaVersionChangeViolationError e:
                                    errorTree.AddSchemaVersionChangeViolations(e);
                                    break;
                                case IInvalidGraphQLSchemaError e:
                                    errorTree.AddGraphQLSchemaErrors(e);
                                    break;
                                case IPersistedQueryValidationError e:
                                    errorTree.AddPersistedQueryValidationErrorsWithClients(e);
                                    break;
                                case IOpenApiCollectionValidationError e:
                                    errorTree.AddOpenApiCollectionValidationErrors(e);
                                    break;
                                case IMcpFeatureCollectionValidationError e:
                                    errorTree.AddMcpFeatureCollectionValidationErrors(e);
                                    break;
                                case IConcurrentOperationError e:
                                    errorTree.AddErrorMessage(e.Message);
                                    break;
                                case IOperationsAreNotAllowedError e:
                                    errorTree.AddErrorMessage(e.Message);
                                    break;
                                case ISchemaVersionSyntaxError e:
                                    errorTree.AddErrorMessage(e.Message);
                                    break;
                                case IProcessingTimeoutError e:
                                    errorTree.AddErrorMessage(e.Message);
                                    break;
                                case IUnexpectedProcessingError e:
                                    errorTree.AddErrorMessage(e.Message);
                                    break;
                                case IError e:
                                    errorTree.AddErrorMessage("Unexpected error: " + e.Message);
                                    break;
                            }
                        }

                        await activity.FailAllAsync(errorTree);

                        throw new ExitException("Schema publish failed.");

                    case ISchemaVersionPublishSuccess:
                        activity.Success($"Published new schema version '{tag.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'.");

                        return ExitCodes.Success;

                    case IProcessingTaskIsReady:
                        activity.Update(Messages.RequestReadyForProcessing);
                        break;

                    case IOperationInProgress:
                        activity.Update(Messages.RequestBeingProcessed);
                        break;

                    case IWaitForApproval waitForApprovalEvent:
                        if (waitForApprovalEvent.Deployment is ISchemaDeployment deployment)
                        {
                            var deploymentErrorTree = new Tree("");

                            foreach (var error in deployment.Errors)
                            {
                                switch (error)
                                {
                                    case IOperationsAreNotAllowedError e:
                                        deploymentErrorTree.AddNode(e.Message);
                                        break;
                                    case ISchemaVersionSyntaxError e:
                                        deploymentErrorTree.AddNode(e.Message);
                                        break;
                                    case ISchemaChangeViolationError e:
                                        deploymentErrorTree.AddSchemaVersionChangeViolations(e);
                                        break;
                                    case IInvalidGraphQLSchemaError e:
                                        deploymentErrorTree.AddGraphQLSchemaErrors(e);
                                        break;
                                    case IPersistedQueryValidationError e:
                                        deploymentErrorTree.AddPersistedQueryValidationErrorsWithClients(e);
                                        break;
                                    case IOpenApiCollectionValidationError e:
                                        deploymentErrorTree.AddOpenApiCollectionValidationErrors(e);
                                        break;
                                    case IMcpFeatureCollectionValidationError e:
                                        deploymentErrorTree.AddMcpFeatureCollectionValidationErrors(e);
                                        break;
                                }
                            }

                            activity.Update(
                                Messages.ValidationFailed,
                                ActivityUpdateKind.Warning,
                                deploymentErrorTree);
                        }

                        activity.Update(
                            Messages.WaitingForApproval,
                            ActivityUpdateKind.Waiting);
                        break;

                    case IProcessingTaskApproved:
                        activity.Update(Messages.RequestApproved);
                        break;

                    default:
                        activity.Update(
                            Messages.UnknownServerResponse, ActivityUpdateKind.Warning);
                        break;
                }
            }

            await activity.FailAllAsync();
        }

        return ExitCodes.Error;
    }
}
