using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Fusion;

internal static class FusionPublishHelpers
{
    public static async Task<string> RequestDeploymentSlotAsync(
        string apiId,
        string stageName,
        string tag,
        string? subgraphId,
        string? subgraphName,
        bool waitForApproval,
        StatusContext? statusContext,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken cancellationToken)
    {
        var input = new BeginFusionConfigurationPublishInput
        {
            ApiId = apiId,
            Tag = tag,
            StageName = stageName,
            SubgraphName = subgraphName,
            SubgraphApiId = subgraphId,
            WaitForApproval = waitForApproval
        };

        var result = await client.BeginFusionConfigurationPublish.ExecuteAsync(input, cancellationToken);
        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.BeginFusionConfigurationPublish.Errors);
        if (data.BeginFusionConfigurationPublish.RequestId is not { } requestId)
        {
            throw Exit("Failed to request deployment slot.");
        }

        console.MarkupLine($"Your request id is [blue]{requestId}[/]");

        using var stopSignal = new Subject<Unit>();
        var subscription = client.OnFusionConfigurationPublishingTaskChanged
            .Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        await subscription.ForEachAsync(OnNext, cancellationToken);

        return requestId;

        void OnNext(IOperationResult<IOnFusionConfigurationPublishingTaskChangedResult> x)
        {
            if (x.Errors is { Count: > 0 } errors)
            {
                console.PrintErrorsAndExit(errors);
                throw Exit("Something went wrong while monitoring the publish task.");
            }

            switch (x.Data?.OnFusionConfigurationPublishingTaskChanged)
            {
                case IProcessingTaskIsQueued v:
                    statusContext?.Status(
                        $"Your request is queued and is in position [blue]{v.QueuePosition}[/].");
                    break;

                case IFusionConfigurationPublishingFailed v:
                    stopSignal.OnNext(Unit.Default);
                    console.PrintErrorsAndExit(v.Errors);
                    throw Exit("Your request has failed.");

                case IFusionConfigurationPublishingSuccess:
                    stopSignal.OnNext(Unit.Default);
                    console.WarningLine("Your request is already published.");
                    break;

                case IProcessingTaskIsReady:
                    stopSignal.OnNext(Unit.Default);
                    console.Success("Your deployment slot is ready.");
                    break;

                case IFusionConfigurationValidationFailed:
                case IFusionConfigurationValidationSuccess:
                case IValidationInProgress:
                case IOperationInProgress:
                case IWaitForApproval:
                case IProcessingTaskApproved:
                    stopSignal.OnNext(Unit.Default);
                    console.Success("Your request is already processing.");
                    break;

                default:
                    throw Exit("Unknown response");
            }
        }
    }

    public static async Task ClaimDeploymentSlot(
        string requestId,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken cancellationToken)
    {
        var input = new StartFusionConfigurationCompositionInput { RequestId = requestId };

        var result =
            await client.StartFusionConfigurationPublish.ExecuteAsync(input, cancellationToken);
        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.StartFusionConfigurationComposition.Errors);
    }

    public static async Task ReleaseDeploymentSlot(
        string requestId,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken cancellationToken)
    {
        var input = new CancelFusionConfigurationCompositionInput { RequestId = requestId };

        var result =
            await client.CancelFusionConfigurationPublish.ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.CancelFusionConfigurationComposition.Errors);
    }

    public static async Task<Stream?> DownloadConfigurationAsync(
        string apiId,
        string stageName,
        IApiClient client,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        var result =
            await client.FetchConfiguration.ExecuteAsync(apiId, stageName, cancellationToken);

        result.EnsureNoErrors();

        var downloadUrl = result.Data?.FusionConfigurationByApiId?.DownloadUrl;

        if (string.IsNullOrEmpty(downloadUrl))
        {
            return null;
        }

        var httpClient = httpClientFactory.CreateClient(ApiClient.ClientName);
        var downloadResult = await httpClient.GetAsync(downloadUrl, cancellationToken);

        downloadResult.EnsureSuccessStatusCode();

        return await downloadResult.Content.ReadAsStreamAsync(cancellationToken);
    }

    public static async Task<bool> UploadConfigurationAsync(
        string requestId,
        Stream stream,
        StatusContext? statusContext,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken cancellationToken)
    {
        var input = new CommitFusionConfigurationPublishInput
        {
            RequestId = requestId, Configuration = new(stream, "gateway.far")
        };

        var result =
            await client.CommitFusionConfigurationPublish.ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.CommitFusionConfigurationPublish.Errors);

        using var stopSignal = new Subject<Unit>();

        var subscription = client.OnFusionConfigurationPublishingTaskChanged
            .Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        var committed = false;

        await foreach (var x in subscription.ToAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            if (x.Errors is { Count: > 0 } errors)
            {
                console.PrintErrorsAndExit(errors);
                throw Exit("No request id returned");
            }

            switch (x.Data?.OnFusionConfigurationPublishingTaskChanged)
            {
                case IProcessingTaskIsQueued v:
                    statusContext?.Status(
                        $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                    break;

                case IFusionConfigurationPublishingFailed v:
                    stopSignal.OnNext(Unit.Default);
                    console.PrintErrorsAndExit(v.Errors);
                    throw Exit("The commit has failed.");

                case IFusionConfigurationPublishingSuccess:
                    committed = true;
                    stopSignal.OnNext(Unit.Default);
                    break;

                case IProcessingTaskIsReady:
                    console.Success("Your request is ready for the committing.");
                    break;

                case IFusionConfigurationValidationFailed:
                    statusContext?.Status(
                        "The validation of your request has failed. Check the errors in Nitro.");
                    break;

                case IFusionConfigurationValidationSuccess:
                    statusContext?.Status("The validation of your request was successful.");
                    break;

                case IValidationInProgress:
                    statusContext?.Status("The validation of your request is in progress.");
                    break;

                case IOperationInProgress:
                    statusContext?.Status("The committing of your request is in progress.");
                    break;

                case IWaitForApproval e:
                    if (e.Deployment is
                        IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Deployment_FusionConfigurationDeployment
                        deployment)
                    {
                        console.PrintErrors(deployment.Errors);
                    }

                    statusContext?.Status(
                        "The committing of your request is waiting for approval. Check Nitro to approve the request.");
                    break;

                case IProcessingTaskApproved:
                    statusContext?.Status("The committing of your request is approved.");

                    break;

                default:
                    statusContext?.Status(
                        "Received an unknown response. Make sure the CLI is on the latest version.");
                    break;
            }
        }

        return committed;
    }
}
