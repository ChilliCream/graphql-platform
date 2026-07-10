using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
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

        await using (var activity = console.StartActivity(
            $"Publishing new version '{tag.EscapeMarkup()}' of OpenAPI collection '{openApiCollectionId.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'",
            "Failed to publish a new OpenAPI collection version."))
        {
            if (force)
            {
                activity.Update(Messages.ForcePushEnabled, ActivityUpdateKind.Warning);
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
                await activity.FailAllAsync();

                foreach (var error in publishRequest.Errors)
                {
                    var errorMessage = error switch
                    {
                        IUnauthorizedOperation err => err.Message,
                        IInvalidSourceMetadataInputError err => err.Message,
                        IStageNotFoundError err => err.Message,
                        IOpenApiCollectionNotFoundError err => err.Message,
                        IOpenApiCollectionVersionNotFoundError err => err.Message,
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

            await foreach (var update in client.SubscribeToOpenApiCollectionPublishAsync(id, ct))
            {
                switch (update)
                {
                    case IProcessingTaskIsQueued v:
                        activity.Update(Messages.QueuedAtPosition(v.QueuePosition), ActivityUpdateKind.Waiting);
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

                        await activity.FailAllAsync(errorTree, "OpenAPI collection version was rejected.");

                        throw new ExitException("OpenAPI collection version was rejected.");

                    case IOpenApiCollectionVersionPublishSuccess:
                        activity.Success($"Published new version '{tag.EscapeMarkup()}' of OpenAPI collection '{openApiCollectionId.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'.");
                        return ExitCodes.Success;

                    case IProcessingTaskIsReady:
                        activity.Update(Messages.RequestReadyForProcessing);
                        break;

                    case IOperationInProgress:
                        activity.Update(Messages.RequestBeingProcessed);
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

                            activity.Update(Messages.ValidationFailed, ActivityUpdateKind.Warning, approvalErrorTree);
                        }

                        activity.Update(Messages.WaitingForApproval, ActivityUpdateKind.Waiting);
                        break;

                    case IProcessingTaskApproved:
                        activity.Update(Messages.RequestApproved);
                        break;

                    default:
                        activity.Update(Messages.UnknownServerResponse, ActivityUpdateKind.Warning);
                        break;
                }
            }

            await activity.FailAllAsync();
        }

        return ExitCodes.Error;
    }
}
