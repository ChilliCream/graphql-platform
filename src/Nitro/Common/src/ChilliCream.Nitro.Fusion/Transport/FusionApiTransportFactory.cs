using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake;

namespace ChilliCream.Nitro.Fusion.Transport;

internal sealed class FusionApiTransportFactory : IFusionDeploymentTransportFactory
{
    internal const int MaxSourceSchemaArchiveSize = 128_000_000;

    public ValueTask<IFusionDeploymentTransport> OpenAsync(
        FusionTarget target,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var services = new ServiceCollection();
        services.AddHttpClient(
            FusionApiClient.ClientName,
            client => ConfigureClient(client, target));
        services.AddFusionApiClient();

        var provider = services.BuildServiceProvider();
        var apiClient = provider.GetRequiredService<IFusionApiClient>();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();

        return ValueTask.FromResult<IFusionDeploymentTransport>(
            new FusionApiTransport(
                target,
                provider,
                apiClient,
                httpClientFactory.CreateClient(FusionApiClient.ClientName)));
    }

    private static void ConfigureClient(HttpClient client, FusionTarget target)
    {
        client.BaseAddress = target.CloudUrl;
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("GraphQL-Preflight", "1");
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "GraphQL-Client-Version",
            GetVersion());
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "ccc-agent",
            $"Nitro Fusion/{GetVersion()}");
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "CCC-api-key",
            target.ApiKey);
        client.DefaultRequestVersion = new Version(2, 0);
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    }

    private static string GetVersion()
    {
        var version = typeof(FusionApiTransportFactory).Assembly.GetName().Version!;
        return new Version(version.Major, version.Minor, version.Build).ToString();
    }

    private sealed class FusionApiTransport(
        FusionTarget target,
        ServiceProvider provider,
        IFusionApiClient apiClient,
        HttpClient httpClient)
        : IFusionDeploymentTransport
    {
        public async Task<byte[]?> DownloadSourceSchemaAsync(
            string name,
            string version,
            CancellationToken cancellationToken)
        {
            const string path =
                "/api/v1/apis/{0}/fusion-subgraphs/{1}/versions/{2}/download";
            var requestUri = string.Format(
                path,
                Uri.EscapeDataString(target.ApiId),
                Uri.EscapeDataString(name),
                Uri.EscapeDataString(version));
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.NotFound)
            {
                return null;
            }

            EnsureSuccess(response);
            return await ReadSourceSchemaArchiveAsync(
                response.Content,
                MaxSourceSchemaArchiveSize,
                cancellationToken);
        }

        public async Task<FusionRemoteCommandResult> UploadSourceSchemaAsync(
            string version,
            string archivePath,
            CancellationToken cancellationToken)
        {
            await using var archive = File.OpenRead(archivePath);
            var input = new UploadFusionSubgraphInput
            {
                ApiId = target.ApiId,
                Tag = version,
                Archive = new Upload(archive, "source-schema.zip")
            };
            var result = await apiClient.UploadFusionSourceSchema.ExecuteAsync(
                input,
                cancellationToken);
            var data = EnsureData(result).UploadFusionSubgraph;
            var errors = GetErrors(data.Errors);

            return new FusionRemoteCommandResult(
                data.FusionSubgraphVersion is not null && errors.Count is 0,
                data.Errors?.Any(e => e.__typename is "DuplicatedTagError") is true,
                errors);
        }

        public async Task<FusionRemoteBeginResult> BeginPublishAsync(
            FusionPublicationRequest request,
            CancellationToken cancellationToken)
        {
            var input = new BeginFusionConfigurationPublishInput
            {
                ApiId = target.ApiId,
                StageName = request.Stage,
                Tag = request.ConfigurationTag,
                WaitForApproval = request.WaitForApproval,
                Subgraphs = request.SourceSchemas
                    .Select(s => new FusionSubgraphVersionInput
                    {
                        Name = s.Name,
                        Tag = s.Version
                    })
                    .ToArray()
            };
            var result = await apiClient.BeginFusionDeployment.ExecuteAsync(
                input,
                cancellationToken);
            var data = EnsureData(result).BeginFusionConfigurationPublish;

            return new FusionRemoteBeginResult(
                data.RequestId,
                GetErrors(data.Errors));
        }

        public async IAsyncEnumerable<FusionRemoteEvent> WatchPublishAsync(
            string requestId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var stopSignal = new ReplaySubject<Unit>(1);
            await using var registration = cancellationToken.Register(
                () =>
                {
                    stopSignal.OnNext(Unit.Default);
                    stopSignal.OnCompleted();
                });
            var subscription = apiClient.WatchFusionDeployment
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            await foreach (var result in subscription.ToAsyncEnumerable())
            {
                var value = EnsureData(result)
                    .OnFusionConfigurationPublishingTaskChanged;
                yield return ToRemoteEvent(value);
            }
        }

        public async Task<FusionRemoteCommandResult> ClaimPublishAsync(
            string requestId,
            CancellationToken cancellationToken)
        {
            var result = await apiClient.ClaimFusionDeployment.ExecuteAsync(
                new StartFusionConfigurationCompositionInput
                {
                    RequestId = requestId
                },
                cancellationToken);
            var data = EnsureData(result).StartFusionConfigurationComposition;
            return ToCommandResult(data.Errors);
        }

        public async Task<FusionRemoteCommandResult> ReleasePublishAsync(
            string requestId,
            CancellationToken cancellationToken)
        {
            var result = await apiClient.ReleaseFusionDeployment.ExecuteAsync(
                new CancelFusionConfigurationCompositionInput
                {
                    RequestId = requestId
                },
                cancellationToken);
            var data = EnsureData(result).CancelFusionConfigurationComposition;
            return ToCommandResult(data.Errors);
        }

        public async Task<FusionRemoteCommandResult> ValidatePublishAsync(
            string requestId,
            ReadOnlyMemory<byte> archive,
            CancellationToken cancellationToken)
        {
            await using var archiveStream = OpenRead(archive);
            var result = await apiClient.ValidateFusionDeployment.ExecuteAsync(
                new ValidateFusionConfigurationCompositionInput
                {
                    RequestId = requestId,
                    Configuration = new Upload(archiveStream, "gateway.far")
                },
                cancellationToken);
            var data = EnsureData(result).ValidateFusionConfigurationComposition;
            return ToCommandResult(data.Errors);
        }

        public async Task<FusionRemoteCommandResult> CommitPublishAsync(
            string requestId,
            ReadOnlyMemory<byte> archive,
            CancellationToken cancellationToken)
        {
            await using var archiveStream = OpenRead(archive);
            var result = await apiClient.CommitFusionDeployment.ExecuteAsync(
                new CommitFusionConfigurationPublishInput
                {
                    RequestId = requestId,
                    Configuration = new Upload(archiveStream, "gateway.far")
                },
                cancellationToken);
            var data = EnsureData(result).CommitFusionConfigurationPublish;
            return ToCommandResult(data.Errors);
        }

        private static Stream OpenRead(ReadOnlyMemory<byte> content)
        {
            if (MemoryMarshal.TryGetArray(
                    content,
                    out ArraySegment<byte> segment))
            {
                return new MemoryStream(
                    segment.Array!,
                    segment.Offset,
                    segment.Count,
                    writable: false,
                    publiclyVisible: true);
            }

            return new MemoryStream(content.ToArray(), writable: false);
        }

        public async ValueTask DisposeAsync()
        {
            httpClient.Dispose();
            await provider.DisposeAsync();
        }

        private static FusionRemoteCommandResult ToCommandResult<TError>(
            IReadOnlyList<TError>? errors)
            where TError : class
        {
            var messages = GetErrors(errors);
            return new FusionRemoteCommandResult(
                messages.Count is 0,
                false,
                messages);
        }

        private static List<string> GetErrors<TError>(
            IReadOnlyList<TError>? errors)
            where TError : class
            => errors?
                .Select(error => error as IFusionError)
                .Where(error => error is not null)
                .Select(error => error!.Message)
                .ToList()
                ?? [];

        private static FusionRemoteEvent ToRemoteEvent(
            IWatchFusionDeployment_OnFusionConfigurationPublishingTaskChanged value)
        {
            var errors = value switch
            {
                IWatchFusionDeployment_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingFailed failed
                    => failed.Errors.Select(error => error.Message).ToArray(),
                IWatchFusionDeployment_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationValidationFailed failed
                    => failed.Errors.Select(error => error.Message).ToArray(),
                _ => []
            };
            var kind = value.__typename switch
            {
                "ProcessingTaskIsReady" => FusionRemoteEventKind.Ready,
                "ProcessingTaskIsQueued" => FusionRemoteEventKind.Queued,
                "FusionConfigurationValidationSuccess"
                    => FusionRemoteEventKind.ValidationSucceeded,
                "FusionConfigurationValidationFailed"
                    => FusionRemoteEventKind.ValidationFailed,
                "FusionConfigurationPublishingSuccess"
                    => FusionRemoteEventKind.PublishingSucceeded,
                "FusionConfigurationPublishingFailed"
                    => FusionRemoteEventKind.PublishingFailed,
                "WaitForApproval" => FusionRemoteEventKind.WaitingForApproval,
                "ProcessingTaskApproved" => FusionRemoteEventKind.Approved,
                "OperationInProgress" or "ValidationInProgress"
                    => FusionRemoteEventKind.InProgress,
                _ => FusionRemoteEventKind.Unknown
            };

            return new FusionRemoteEvent(kind, errors);
        }

        private static TData EnsureData<TData>(IOperationResult<TData> result)
            where TData : class
        {
            if (result.Errors is { Count: > 0 } errors)
            {
                throw new FusionDeploymentException(
                    $"Nitro GraphQL request failed: {errors[0].Message}");
            }

            return result.Data
                ?? throw new FusionDeploymentException(
                    "Nitro GraphQL request returned no data.");
        }

        private static void EnsureSuccess(HttpResponseMessage response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden)
            {
                throw new FusionDeploymentException(
                    "Nitro rejected the supplied API key.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new FusionDeploymentException(
                    $"Nitro returned HTTP {(int)response.StatusCode} "
                    + $"({response.ReasonPhrase}).");
            }
        }
    }

    internal static Task<byte[]> ReadSourceSchemaArchiveAsync(
        HttpContent content,
        int maxArchiveSize,
        CancellationToken cancellationToken)
        => ReadSourceSchemaArchiveAsync(
            content,
            maxArchiveSize,
            ArrayPool<byte>.Shared,
            81920,
            cancellationToken);

    internal static async Task<byte[]> ReadSourceSchemaArchiveAsync(
        HttpContent content,
        int maxArchiveSize,
        ArrayPool<byte> bufferPool,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(bufferPool);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxArchiveSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        if (content.Headers.ContentLength is > 0 and var contentLength
            && contentLength > maxArchiveSize)
        {
            throw ArchiveResponseTooLarge(maxArchiveSize);
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        var chunks = new List<(byte[] Buffer, int Length)>();
        var length = 0;

        try
        {
            var endOfStream = false;
            while (!endOfStream)
            {
                var remaining = maxArchiveSize - length;
                var readSize = remaining >= bufferSize
                    ? bufferSize
                    : remaining + 1;
                var buffer = bufferPool.Rent(readSize);
                var bufferLength = 0;
                var retained = false;

                try
                {
                    while (bufferLength < readSize)
                    {
                        var bytesRead = await stream.ReadAsync(
                            buffer.AsMemory(bufferLength, readSize - bufferLength),
                            cancellationToken);

                        if (bytesRead is 0)
                        {
                            endOfStream = true;
                            break;
                        }

                        bufferLength += bytesRead;
                    }

                    if (bufferLength is 0)
                    {
                        continue;
                    }

                    if (bufferLength > maxArchiveSize - length)
                    {
                        throw ArchiveResponseTooLarge(maxArchiveSize);
                    }

                    length += bufferLength;
                    chunks.Add((buffer, bufferLength));
                    retained = true;
                }
                finally
                {
                    if (!retained)
                    {
                        bufferPool.Return(buffer, clearArray: true);
                    }
                }
            }

            var archive = GC.AllocateUninitializedArray<byte>(length);
            var offset = 0;
            foreach (var chunk in chunks)
            {
                chunk.Buffer.AsSpan(0, chunk.Length).CopyTo(archive.AsSpan(offset));
                offset += chunk.Length;
            }

            return archive;
        }
        finally
        {
            foreach (var chunk in chunks)
            {
                bufferPool.Return(chunk.Buffer, clearArray: true);
            }
        }
    }

    private static FusionDeploymentException ArchiveResponseTooLarge(
        int maxArchiveSize)
        => new(
            "The compressed Fusion source schema archive exceeds the maximum "
            + $"allowed size of {maxArchiveSize} bytes.");
}
