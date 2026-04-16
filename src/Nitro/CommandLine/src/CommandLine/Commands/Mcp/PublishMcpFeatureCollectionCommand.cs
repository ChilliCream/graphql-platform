using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
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
            mcp publish \
              --mcp-feature-collection-id "<collection-id>" \
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
        var client = services.GetRequiredService<IMcpClient>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetRequiredValue(Opt<TagOption>.Instance);
        var stage = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var mcpFeatureCollectionId = parseResult.GetRequiredValue(Opt<McpFeatureCollectionIdOption>.Instance);
        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
        var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity(
            $"Publishing new version '{tag.EscapeMarkup()}' of MCP feature collection '{mcpFeatureCollectionId.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'",
            "Failed to publish a new MCP feature collection version."))
        {
            if (force)
            {
                activity.Update(Messages.ForcePushEnabled, ActivityUpdateKind.Warning);
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
                await activity.FailAllAsync();

                foreach (var error in publishRequest.Errors)
                {
                    var errorMessage = error switch
                    {
                        IUnauthorizedOperation err => err.Message,
                        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_InvalidSourceMetadataInputError err => err.Message,
                        IStageNotFoundError err => err.Message,
                        IMcpFeatureCollectionNotFoundError err => err.Message,
                        IMcpFeatureCollectionVersionNotFoundError err => err.Message,
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

            await foreach (var update in client.SubscribeToMcpFeatureCollectionPublishAsync(id, ct))
            {
                switch (update)
                {
                    case IProcessingTaskIsQueued v:
                        activity.Update(Messages.QueuedAtPosition(v.QueuePosition), ActivityUpdateKind.Waiting);
                        break;

                    case IMcpFeatureCollectionVersionPublishFailed { Errors: var errors }:
                        var errorTree = new Tree("");

                        foreach (var error in errors)
                        {
                            switch (error)
                            {
                                case IMcpFeatureCollectionValidationError e:
                                    errorTree.AddMcpFeatureCollectionValidationErrors(e);
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

                        activity.Fail(errorTree);

                        throw new ExitException("MCP feature collection publish failed.");

                    case IMcpFeatureCollectionVersionPublishSuccess:
                        activity.Success($"Published new version '{tag.EscapeMarkup()}' of MCP feature collection '{mcpFeatureCollectionId.EscapeMarkup()}' to stage '{stage.EscapeMarkup()}'.");
                        return ExitCodes.Success;

                    case IProcessingTaskIsReady:
                        activity.Update(Messages.RequestReadyForProcessing);
                        break;

                    case IOperationInProgress:
                        activity.Update(Messages.RequestBeingProcessed);
                        break;

                    case IWaitForApproval waitForApprovalEvent:
                        if (waitForApprovalEvent.Deployment is IMcpFeatureCollectionDeployment deployment)
                        {
                            var approvalErrorTree = new Tree("");

                            foreach (var error in deployment.Errors)
                            {
                                switch (error)
                                {
                                    case IMcpFeatureCollectionValidationError e:
                                        approvalErrorTree.AddMcpFeatureCollectionValidationErrors(e);
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
