using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using StrawberryShake;

namespace ChilliCream.Nitro.Client.FusionConfiguration;

internal sealed class FusionConfigurationClient(
    IApiClient apiClient,
    IHttpClientFactory httpClientFactory)
    : IFusionConfigurationClient
{
    public async Task<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish> RequestDeploymentSlotAsync(
        string apiId,
        string stageName,
        string tag,
        string? subgraphId,
        string? subgraphName,
        SourceSchemaVersion[]? sourceSchemaVersions,
        bool waitForApproval,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new BeginFusionConfigurationPublishInput
        {
            ApiId = apiId,
            Tag = tag,
            StageName = stageName,
            SubgraphName = subgraphName,
            SubgraphApiId = subgraphId,
            WaitForApproval = waitForApproval,
            Source = SourceMetadataMapper.Map(source),
            Subgraphs = sourceSchemaVersions is { Length: > 0 }
                ? sourceSchemaVersions
                    .Select(x => new FusionSubgraphVersionInput
                    {
                        Name = x.Name,
                        Tag = x.Version
                    })
                    .ToArray()
                : null
        };

        var result = await apiClient.BeginFusionConfigurationPublish.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).BeginFusionConfigurationPublish;
    }

    public async IAsyncEnumerable<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged>
        SubscribeToFusionConfigurationPublishingTaskChangedAsync(
            string requestId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stopSignal = new ReplaySubject<Unit>(1);
        await using var _ = cancellationToken.Register(stopSignal);

        var subscription = apiClient.OnFusionConfigurationPublishingTaskChanged
            .Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        // The cancellation token is intentionally not passed to ToAsyncEnumerable() to avoid
        // an OperationCanceledException. Cancellation is handled via the stop signal above,
        // which completes the sequence cleanly.
        await foreach (var @event in subscription.ToAsyncEnumerable())
        {
            var data = OperationResultHelper.EnsureData(@event);
            yield return data.OnFusionConfigurationPublishingTaskChanged;
        }
    }

    public async Task<IStartFusionConfigurationPublish_StartFusionConfigurationComposition> ClaimDeploymentSlotAsync(
        string requestId,
        CancellationToken cancellationToken)
    {
        var input = new StartFusionConfigurationCompositionInput { RequestId = requestId };
        var result = await apiClient.StartFusionConfigurationPublish.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).StartFusionConfigurationComposition;
    }

    public async Task<ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition> ReleaseDeploymentSlotAsync(
        string requestId,
        CancellationToken cancellationToken)
    {
        var input = new CancelFusionConfigurationCompositionInput { RequestId = requestId };
        var result = await apiClient.CancelFusionConfigurationPublish.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).CancelFusionConfigurationComposition;
    }

    public async Task<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish> CommitFusionArchiveAsync(
        string requestId,
        Stream stream,
        CancellationToken cancellationToken)
    {
        var input = new CommitFusionConfigurationPublishInput
        {
            RequestId = requestId,
            Configuration = new Upload(stream, "gateway.far")
        };

        var result = await apiClient.CommitFusionConfigurationPublish.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).CommitFusionConfigurationPublish;
    }

    public async Task<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition> ValidateFusionConfigurationPublishAsync(
        string requestId,
        Stream stream,
        CancellationToken cancellationToken)
    {
        var input = new ValidateFusionConfigurationCompositionInput
        {
            RequestId = requestId,
            Configuration = new Upload(stream, "gateway.fgp")
        };

        var result = await apiClient.ValidateFusionConfigurationPublish.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).ValidateFusionConfigurationComposition;
    }

    public async Task<IUploadFusionSubgraph_UploadFusionSubgraph> UploadFusionSubgraphAsync(
        string apiId,
        string tag,
        Stream archive,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new UploadFusionSubgraphInput
        {
            Archive = new Upload(archive, "source-schema.zip"),
            ApiId = apiId,
            Tag = tag,
            Source = SourceMetadataMapper.Map(source)
        };

        var result = await apiClient.UploadFusionSubgraph.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).UploadFusionSubgraph;
    }

    public async Task<Stream?> DownloadSourceSchemaArchiveAsync(
        string apiId,
        string sourceSchemaName,
        string sourceSchemaVersion,
        CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(ApiClient.ClientName);
        var request = CreateDownloadSourceSchemaVersionRequest(apiId, sourceSchemaName, sourceSchemaVersion);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new NitroClientAuthorizationException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new NitroClientHttpRequestException(response.StatusCode);
        }

        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task<Stream?> DownloadLatestFusionArchiveAsync(
        string apiId,
        string stageName,
        string archiveVersion,
        string archiveFormat,
        CancellationToken cancellationToken)
    {
        var request = CreateDownloadLatestFusionArchiveRequest(apiId, stageName, archiveVersion, archiveFormat);

        using var httpClient = httpClientFactory.CreateClient(ApiClient.ClientName);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new NitroClientAuthorizationException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new NitroClientHttpRequestException(response.StatusCode);
        }

        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    private static HttpRequestMessage CreateDownloadLatestFusionArchiveRequest(
        string apiId,
        string stageName,
        string archiveVersion,
        string archiveFormat)
    {
        var escapedApiId = Uri.EscapeDataString(apiId);
        var escapedStageName = Uri.EscapeDataString(stageName);

        var requestUri = $"/api/v1/apis/{escapedApiId}/fusion/configurations/latest/download"
            + $"?stage={escapedStageName}"
            + $"&format={archiveFormat}"
            + $"&fusionVersion={Uri.EscapeDataString(archiveVersion)}";

        return new HttpRequestMessage(HttpMethod.Get, requestUri);
    }

    private static HttpRequestMessage CreateDownloadSourceSchemaVersionRequest(
        string apiId,
        string sourceSchemaName,
        string sourceSchemaVersion)
    {
        const string path = "/api/v1/apis/{0}/fusion-subgraphs/{1}/versions/{2}/download";
        var escapedApiId = Uri.EscapeDataString(apiId);
        var escapedSourceSchemaName = Uri.EscapeDataString(sourceSchemaName);
        var escapedSourceSchemaVersion = Uri.EscapeDataString(sourceSchemaVersion);
        var requestUri = string.Format(path, escapedApiId, escapedSourceSchemaName, escapedSourceSchemaVersion);
        return new HttpRequestMessage(HttpMethod.Get, requestUri);
    }
}
