using System.Text.Json;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.FusionCompatibility;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
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

        activity.Update($"Publication request created. {$"(ID: {requestId.EscapeMarkup()})".Dim()}");

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

                    await activity.FailAllAsync(errorTree, "Fusion configuration version was rejected.");

                    throw new ExitException("Fusion configuration version was rejected.");

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

                    await activity.FailAllAsync(publishErrorTree, "Fusion configuration version was rejected.");
                    throw new ExitException("Fusion configuration version was rejected.");

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

        await foreach (var @event in client
                           .SubscribeToFusionConfigurationPublishingTaskChangedAsync(requestId, cancellationToken))
        {
            switch (@event)
            {
                case IProcessingTaskIsQueued:
                    throw Exit(
                        "Your request is in the queued state. Try to run `fusion-configuration publish start` once the request is ready.");

                case IFusionConfigurationPublishingFailed:
                    throw Exit("Your request has already failed.");

                case IFusionConfigurationPublishingSuccess:
                    throw Exit("Your request is already published.");

                case IProcessingTaskIsReady:
                    throw Exit(
                        "Your request is ready for the composition. Run `fusion-configuration publish start`.");

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

                    activity.Fail(errorTree, "Fusion configuration failed validation.");

                    return false;

                case IFusionConfigurationValidationSuccess:
                    return true;

                case IOperationInProgress:
                case IValidationInProgress:
                case IWaitForApproval:
                case IProcessingTaskApproved:
                    activity.Update(Messages.Validating);
                    break;

                default:
                    activity.Update(Messages.UnknownServerResponse, ActivityUpdateKind.Warning);
                    break;
            }
        }

        return false;
    }

    public static async Task<Stream> PrepareComposedArchiveAsync(
        INitroConsoleActivity activity,
        string apiId,
        string stageName,
        string? legacyArchiveFile,
        Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas,
        IFusionConfigurationClient client,
        IFileSystem fileSystem,
        INitroConsole console,
        CancellationToken cancellationToken)
    {
        Stream? existingArchiveStream;
        MemoryStream? legacyBuffer = null;
        CompositionSettings? compositionSettings = null;

        await using (var downloadActivity = activity.StartChildActivity(
                         $"Downloading existing configuration from '{stageName}'",
                         "Failed to download the existing Fusion configuration."))
        {
            existingArchiveStream = await client.DownloadLatestFusionArchiveAsync(
                apiId,
                stageName,
                WellKnownVersions.LatestGatewayFormatVersion.ToString(),
                ArchiveFormats.Far,
                cancellationToken);

            // Precedence:
            //   server .far + no flag -> use .far (existing embedded .fgp carried forward via Update mode)
            //   server .far + flag    -> use .far, refresh embedded .fgp from local flag
            //   server .fgp + no flag -> error (server .fgp may be outdated, require explicit local flag)
            //   server .fgp + flag    -> use local flag as migration source
            //   nothing + flag        -> use local flag as composition base
            //   nothing + no flag     -> fresh compose
            if (existingArchiveStream is not null)
            {
                downloadActivity.Success($"Downloaded existing configuration from '{stageName}'.");
            }
            else if (legacyArchiveFile is not null)
            {
                downloadActivity.Warning(
                    $"There is no existing configuration on '{stageName.EscapeMarkup()}', using {OptionalLegacyFusionArchiveFileOption.OptionName} instead.");
            }
            else
            {
                var serverLegacyStream = await client.DownloadLatestFusionArchiveAsync(
                    apiId,
                    stageName,
                    WellKnownVersions.LegacyGatewayFormatVersion.ToString(),
                    ArchiveFormats.Fgp,
                    cancellationToken);

                if (serverLegacyStream is not null)
                {
                    await serverLegacyStream.DisposeAsync();

                    throw new ExitException(
                        Messages.LegacyArchiveRequiredForFgpStage(stageName));
                }

                downloadActivity.Warning($"There is no existing configuration on '{stageName}'.");
            }
        }

        if (legacyArchiveFile is not null)
        {
            try
            {
                await using var fs = fileSystem.OpenReadStream(legacyArchiveFile);
                legacyBuffer = new MemoryStream();
                await fs.CopyToAsync(legacyBuffer, cancellationToken);
                legacyBuffer.Position = 0;
            }
            catch (IOException ex)
            {
                throw new ExitException(
                    Messages.FailedToOpenLegacyArchive(legacyArchiveFile, ex.Message));
            }
        }

        await using var composeActivity = activity.StartChildActivity(
            "Composing new configuration",
            "Failed to compose new configuration.");

        try
        {
            if (legacyBuffer is not null && existingArchiveStream is null)
            {
                try
                {
                    var migratedSettings = await LegacyFusionArchiveMigrator.MergeIntoAsync(
                        legacyBuffer,
                        newSourceSchemas,
                        newSourceSchemas.Keys,
                        cancellationToken);

                    compositionSettings = new CompositionSettings().MergeInto(
                        migratedSettings ?? new CompositionSettings());
                }
                catch (FusionGraphPackageException ex) when (legacyArchiveFile is not null)
                {
                    throw new ExitException(
                        Messages.FailedToOpenLegacyArchive(legacyArchiveFile, ex.Message));
                }
            }

            var archiveStream = new MemoryStream();
            var (result, compositionLog) = await ComposeAsync(
                archiveStream,
                existingArchiveStream,
                stageName,
                newSourceSchemas,
                compositionSettings,
                legacyBuffer,
                cancellationToken);

            if (result.IsSuccess)
            {
                composeActivity.Success("Composed new configuration.");

                return archiveStream;
            }

            await composeActivity.FailAllAsync();

            console.WriteLine();
            console.WriteLine("## Composition log");
            console.WriteLine();

            FusionComposeCommand.WriteCompositionLog(
                compositionLog,
                console.Out,
                false);

            foreach (var error in result.Errors)
            {
                console.Error.WriteErrorLine(error.Message);
            }

            throw new ExitException();
        }
        finally
        {
            if (legacyBuffer is not null)
            {
                await legacyBuffer.DisposeAsync();
            }
        }
    }

    public static async Task<(CompositionResult<MutableSchemaDefinition>, CompositionLog)> ComposeAsync(
        Stream archiveStream,
        Stream? existingArchiveStream,
        string environment,
        Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas,
        CompositionSettings? compositionSettings,
        Stream? legacyArchive,
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
            legacyArchive,
            cancellationToken);

        archiveStream.Seek(0, SeekOrigin.Begin);

        return result;
    }

    public static async Task<(CompositionResult<MutableSchemaDefinition>, CompositionLog)> ComposeAsync(
        FusionArchive archive,
        string environment,
        Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas,
        CompositionSettings? compositionSettings,
        Stream? legacyArchive,
        CancellationToken cancellationToken)
    {
        var compositionLog = new CompositionLog();

        var result = await CompositionHelper.ComposeAsync(
            compositionLog,
            newSourceSchemas,
            archive,
            environment,
            compositionSettings,
            legacyArchive,
            cancellationToken);

        return (result, compositionLog);
    }
}
