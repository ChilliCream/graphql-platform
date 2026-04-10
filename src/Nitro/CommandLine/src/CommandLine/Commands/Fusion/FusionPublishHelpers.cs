using System.Text.Json;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal static class FusionPublishHelpers
{
    public static async Task<string> RequestDeploymentSlotAsync(
        string apiId,
        string stageName,
        string tag,
        string? subgraphId,
        string? subgraphName,
        SourceSchemaVersion[]? sourceSchemaVersions,
        bool waitForApproval,
        SourceMetadata? source,
        INitroConsoleActivity activity,
        INitroConsole console,
        IFusionConfigurationClient client,
        CancellationToken cancellationToken)
    {
        var deploymentSlotRequest = await client.RequestDeploymentSlotAsync(
            apiId,
            stageName,
            tag,
            subgraphId,
            subgraphName,
            sourceSchemaVersions,
            waitForApproval,
            source,
            cancellationToken);

        if (deploymentSlotRequest.Errors?.Count > 0)
        {
            await activity.FailAllAsync();

            foreach (var error in deploymentSlotRequest.Errors)
            {
                var errorMessage = error switch
                {
                    IUnauthorizedOperation err => err.Message,
                    IInvalidSourceMetadataInputError err => err.Message,
                    IApiNotFoundError err => err.Message,
                    IStageNotFoundError err => err.Message,
                    ISubgraphInvalidError err => err.Message,
                    IInvalidProcessingStateTransitionError err => err.Message,
                    IError err => Messages.UnexpectedMutationError(err),
                    _ => Messages.UnexpectedMutationError()
                };

                console.Error.WriteErrorLine(errorMessage);
            }

            throw new ExitException();
        }

        var requestId = deploymentSlotRequest.RequestId;

        if (string.IsNullOrEmpty(requestId))
        {
            throw MutationReturnedNoData();
        }

        // activity.Update($"Request ID: {requestId.EscapeMarkup()}");

        using var subscriptionCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await foreach (var @event in client
                           .SubscribeToFusionConfigurationPublishingTaskChangedAsync(
                               requestId,
                               subscriptionCancellation.Token))
        {
            switch (@event)
            {
                case IProcessingTaskIsQueued v:
                    activity.Update(Messages.QueuedAtPosition(v.QueuePosition), ActivityUpdateKind.Waiting);
                    break;

                case IFusionConfigurationPublishingFailed v:
                    await subscriptionCancellation.CancelAsync();

                    var errorTree = new Tree("");

                    foreach (var error in v.Errors)
                    {
                        switch (error)
                        {
                            case IInvalidGraphQLSchemaError e:
                                errorTree.AddGraphQLSchemaErrors(e);
                                break;

                            default:
                                errorTree.AddErrorMessage(error.Message);
                                break;
                        }
                    }

                    activity.Fail(errorTree);
                    throw Exit("Your request has failed.");

                case IFusionConfigurationPublishingSuccess:
                    await subscriptionCancellation.CancelAsync();
                    activity.Update("Already published.", ActivityUpdateKind.Warning);
                    break;

                case IProcessingTaskIsReady:
                    await subscriptionCancellation.CancelAsync();

                    return requestId;

                case IFusionConfigurationValidationFailed:
                case IFusionConfigurationValidationSuccess:
                case IValidationInProgress:
                case IOperationInProgress:
                case IWaitForApproval:
                case IProcessingTaskApproved:
                    await subscriptionCancellation.CancelAsync();
                    activity.Update("Already processing.");
                    break;

                default:
                    activity.Update(Messages.UnknownServerResponse, ActivityUpdateKind.Warning);
                    break;
            }
        }

        throw new ExitException("Subscription terminated before request was ready for processing.");
    }

    public static async Task ClaimDeploymentSlotAsync(
        string requestId,
        INitroConsoleActivity activity,
        INitroConsole console,
        IFusionConfigurationClient client,
        CancellationToken cancellationToken)
    {
        var result = await client.ClaimDeploymentSlotAsync(requestId, cancellationToken);

        if (result.Errors?.Count > 0)
        {
            await activity.FailAllAsync();

            foreach (var error in result.Errors)
            {
                var errorMessage = error switch
                {
                    IUnauthorizedOperation err => err.Message,
                    IFusionConfigurationRequestNotFoundError err => err.Message,
                    IInvalidProcessingStateTransitionError err => err.Message,
                    IError err => Messages.UnexpectedMutationError(err),
                    _ => Messages.UnexpectedMutationError()
                };

                console.Error.WriteErrorLine(errorMessage);
            }

            throw new ExitException();
        }
    }

    public static async Task<bool> UploadFusionConfigurationAsync(
        string requestId,
        Stream stream,
        INitroConsoleActivity activity,
        INitroConsole console,
        IFusionConfigurationClient client,
        CancellationToken cancellationToken)
    {
        var commitResult = await client.CommitFusionArchiveAsync(requestId, stream, cancellationToken);

        if (commitResult.Errors?.Count > 0)
        {
            await activity.FailAllAsync();

            foreach (var error in commitResult.Errors)
            {
                var errorMessage = error switch
                {
                    IUnauthorizedOperation err => err.Message,
                    IFusionConfigurationRequestNotFoundError err => err.Message,
                    IInvalidProcessingStateTransitionError err => err.Message,
                    IError err => Messages.UnexpectedMutationError(err),
                    _ => Messages.UnexpectedMutationError()
                };

                console.Error.WriteErrorLine(errorMessage);
            }

            throw new ExitException();
        }

        var committed = false;

        await foreach (var @event in client
                           .SubscribeToFusionConfigurationPublishingTaskChangedAsync(requestId, cancellationToken))
        {
            switch (@event)
            {
                case IProcessingTaskIsQueued v:
                    activity.Update(Messages.QueuedAtPosition(v.QueuePosition), ActivityUpdateKind.Waiting);
                    break;

                case IFusionConfigurationPublishingFailed v:
                    var publishErrorTree = new Tree("");

                    foreach (var error in v.Errors)
                    {
                        switch (error)
                        {
                            case IInvalidGraphQLSchemaError e:
                                publishErrorTree.AddGraphQLSchemaErrors(e);
                                break;

                            default:
                                publishErrorTree.AddErrorMessage(error.Message);
                                break;
                        }
                    }

                    activity.Fail(publishErrorTree);
                    throw new ExitException("Failed to publish the new configuration.");

                case IFusionConfigurationPublishingSuccess:
                    committed = true;
                    return committed;

                case IProcessingTaskIsReady:
                    activity.Update(Messages.RequestReadyForProcessing);
                    break;

                case IFusionConfigurationValidationFailed:
                    activity.Update(Messages.ValidationFailed);
                    break;

                case IFusionConfigurationValidationSuccess:
                    activity.Update(Messages.ValidationPassed);
                    break;

                case IValidationInProgress:
                    activity.Update(Messages.Validating);
                    break;

                case IOperationInProgress:
                    activity.Update(Messages.RequestBeingProcessed);
                    break;

                case IWaitForApproval waitForApprovalEvent:
                    if (waitForApprovalEvent.Deployment is IFusionConfigurationDeployment deployment)
                    {
                        var errorTree = new Tree("");

                        foreach (var error in deployment.Errors)
                        {
                            switch (error)
                            {
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
                            }
                        }

                        activity.Update(Messages.ValidationFailed, ActivityUpdateKind.Warning, errorTree);
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

        return committed;
    }

    public static async Task<bool> ValidateFusionConfigurationAsync(
        string requestId,
        Stream stream,
        INitroConsoleActivity activity,
        INitroConsole console,
        IFusionConfigurationClient client,
        CancellationToken cancellationToken)
    {
        var result = await client.ValidateFusionConfigurationPublishAsync(
            requestId,
            stream,
            cancellationToken);

        if (result.Errors?.Count > 0)
        {
            activity.Fail();

            foreach (var error in result.Errors)
            {
                var errorMessage = error switch
                {
                    IUnauthorizedOperation err => err.Message,
                    IFusionConfigurationRequestNotFoundError err => err.Message,
                    IInvalidProcessingStateTransitionError err => err.Message,
                    IError err => Messages.UnexpectedMutationError(err),
                    _ => Messages.UnexpectedMutationError()
                };

                console.Error.WriteErrorLine(errorMessage);
            }

            throw new ExitException();
        }

        await foreach (var @event in client
                           .SubscribeToFusionConfigurationPublishingTaskChangedAsync(requestId, cancellationToken))
        {
            switch (@event)
            {
                case IProcessingTaskIsQueued:
                    throw Exit(
                        "Your request is in the queued state. Try to run `fusion-configuration publish start` once the request is ready ");

                case IFusionConfigurationPublishingFailed:
                    throw Exit("Your request has already failed");

                case IFusionConfigurationPublishingSuccess:
                    throw Exit("You request is already published");

                case IProcessingTaskIsReady:
                    throw Exit(
                        "Your request is ready for the composition. Run `fusion-configuration publish start`");

                case IFusionConfigurationValidationFailed { Errors: var errors }:
                    var errorTree = new Tree("");

                    foreach (var error in errors)
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
                            case IUnexpectedProcessingError e:
                                errorTree.AddErrorMessage(e.Message);
                                break;
                        }
                    }

                    activity.Fail(errorTree);
                    return false;

                case IFusionConfigurationValidationSuccess:
                    return true;

                case IOperationInProgress:
                case IValidationInProgress:
                case IWaitForApproval:
                case IProcessingTaskApproved:
                    // activity.Update(Messages.Validating);
                    break;

                default:
                    activity.Update(Messages.UnknownServerResponse, ActivityUpdateKind.Warning);
                    break;
            }
        }

        return false;
    }

    public static async Task<(CompositionResult<MutableSchemaDefinition>, CompositionLog)> ComposeAsync(
        Stream archiveStream,
        Stream? existingArchiveStream,
        string environment,
        Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas,
        CompositionSettings? compositionSettings,
        CancellationToken cancellationToken)
    {
        FusionArchive archive;

        if (existingArchiveStream is not null)
        {
            await existingArchiveStream.CopyToAsync(archiveStream, cancellationToken);
            await existingArchiveStream.DisposeAsync();

            archiveStream.Seek(0, SeekOrigin.Begin);

            archive = FusionArchive.Open(
                archiveStream,
                mode: FusionArchiveMode.Update,
                leaveOpen: true);
        }
        else
        {
            archive = FusionArchive.Create(archiveStream, leaveOpen: true);
        }

        var result = await ComposeAsync(
            archive,
            environment,
            newSourceSchemas,
            compositionSettings,
            cancellationToken);

        archiveStream.Seek(0, SeekOrigin.Begin);

        return result;
    }

    public static async Task<(CompositionResult<MutableSchemaDefinition>, CompositionLog)> ComposeAsync(
        FusionArchive archive,
        string environment,
        Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas,
        CompositionSettings? compositionSettings,
        CancellationToken cancellationToken)
    {
        var compositionLog = new CompositionLog();

        var result = await CompositionHelper.ComposeAsync(
            compositionLog,
            newSourceSchemas,
            archive,
            environment,
            compositionSettings,
            cancellationToken);

        return (result, compositionLog);
    }
}
