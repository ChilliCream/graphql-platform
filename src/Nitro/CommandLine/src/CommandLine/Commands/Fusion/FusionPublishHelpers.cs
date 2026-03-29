using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
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
            activity.Fail();

            foreach (var error in deploymentSlotRequest.Errors)
            {
                var errorMessage = error switch
                {
                    IUnauthorizedOperation err => err.Message,
                    IApiNotFoundError err => err.Message,
                    IStageNotFoundError err => err.Message,
                    ISubgraphInvalidError err => err.Message,
                    IInvalidProcessingStateTransitionError err => err.Message,
                    IError err => "Unexpected mutation error: " + err.Message,
                    _ => "Unexpected mutation error."
                };

                await console.Error.WriteLineAsync(errorMessage);
            }

            throw Exit("Failed to request deployment slot.");
        }

        var requestId = deploymentSlotRequest.RequestId;

        if (string.IsNullOrEmpty(requestId))
        {
            throw Exit("Failed to request deployment slot.");
        }

        console.MarkupLine($"Your request ID is [blue]{requestId}[/]");

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
                    activity.Update(
                        $"Your request is queued and is in position [blue]{v.QueuePosition}[/].");
                    break;

                case IFusionConfigurationPublishingFailed v:
                    await subscriptionCancellation.CancelAsync();
                    activity.Fail();

                    foreach (var error in v.Errors)
                    {
                        switch (error)
                        {
                            case IInvalidGraphQLSchemaError e:
                                console.PrintGraphQLSchemaErrors(e);
                                break;

                            default:
                                await console.Error.WriteLineAsync(error.Message);
                                break;
                        }
                    }

                    await console.Error.WriteLineAsync("Your request has failed.");
                    throw Exit("Your request has failed.");

                case IFusionConfigurationPublishingSuccess:
                    await subscriptionCancellation.CancelAsync();
                    console.WarningLine("Your request is already published.");
                    break;

                case IProcessingTaskIsReady:
                    await subscriptionCancellation.CancelAsync();
                    console.Success("Your deployment slot is ready.");
                    break;

                case IFusionConfigurationValidationFailed:
                case IFusionConfigurationValidationSuccess:
                case IValidationInProgress:
                case IOperationInProgress:
                case IWaitForApproval:
                case IProcessingTaskApproved:
                    await subscriptionCancellation.CancelAsync();
                    console.Success("Your request is already processing.");
                    break;

                default:
                    throw Exit("Unknown response");
            }
        }

        return requestId;
    }

    public static async Task<bool> UploadFusionArchiveAsync(
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
            activity.Fail();

            foreach (var error in commitResult.Errors)
            {
                var errorMessage = error switch
                {
                    IUnauthorizedOperation err => err.Message,
                    IFusionConfigurationRequestNotFoundError err => err.Message,
                    IInvalidProcessingStateTransitionError err => err.Message,
                    IError err => "Unexpected mutation error: " + err.Message,
                    _ => "Unexpected mutation error."
                };

                await console.Error.WriteLineAsync(errorMessage);
            }

            throw Exit("Failed to commit Fusion archive.");
        }

        var committed = false;

        await foreach (var @event in client
            .SubscribeToFusionConfigurationPublishingTaskChangedAsync(requestId, cancellationToken))
        {
            switch (@event)
            {
                case IProcessingTaskIsQueued v:
                    activity.Update(
                        $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                    break;

                case IFusionConfigurationPublishingFailed v:
                    activity.Fail();

                    foreach (var error in v.Errors)
                    {
                        switch (error)
                        {
                            case IInvalidGraphQLSchemaError e:
                                console.PrintGraphQLSchemaErrors(e);
                                break;

                            default:
                                await console.Error.WriteLineAsync(error.Message);
                                break;
                        }
                    }

                    await console.Error.WriteLineAsync("The commit has failed.");
                    throw Exit("The commit has failed.");

                case IFusionConfigurationPublishingSuccess:
                    committed = true;
                    return committed;

                case IProcessingTaskIsReady:
                    console.Success("Your request is ready for the committing.");
                    break;

                case IFusionConfigurationValidationFailed:
                    activity.Update(
                        "The validation of your request has failed. Check the errors in Nitro.");
                    break;

                case IFusionConfigurationValidationSuccess:
                    activity.Update("The validation of your request was successful.");
                    break;

                case IValidationInProgress:
                    activity.Update("The validation of your request is in progress.");
                    break;

                case IOperationInProgress:
                    activity.Update("The committing of your request is in progress.");
                    break;

                case IWaitForApproval:
                    activity.Update(
                        "The committing of your request is waiting for approval. Check Nitro to approve the request.");
                    break;

                case IProcessingTaskApproved:
                    activity.Update("The committing of your request is approved.");
                    break;

                default:
                    activity.Update(
                        "Received an unknown response. Make sure the CLI is on the latest version.");
                    break;
            }
        }

        return committed;
    }

    public static async Task<bool> ComposeAsync(
        Stream archiveStream,
        Stream? existingArchiveStream,
        string environment,
        Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas,
        CompositionSettings? compositionSettings,
        INitroConsole console,
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
            console,
            cancellationToken);

        archiveStream.Seek(0, SeekOrigin.Begin);

        return result;
    }

    public static async Task<bool> ComposeAsync(
        FusionArchive archive,
        string environment,
        Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas,
        CompositionSettings? compositionSettings,
        INitroConsole console,
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

        FusionComposeCommand.WriteCompositionLog(
            compositionLog,
            Console.Out,
            false);

        if (result.IsFailure)
        {
            foreach (var error in result.Errors)
            {
                console.WriteLine(error.Message);
            }

            return false;
        }

        return true;
    }
}
