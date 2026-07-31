using System.Buffers;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Sends the Nitro API operations of the Fusion deployment workflow. Every operation is bounded
/// by the cancellation token of its caller, and results are read as JSON without an intermediate
/// object model.
/// </summary>
internal sealed class NitroFusionApi : IDisposable
{
    internal const int MaxSourceSchemaArchiveSize = 128_000_000;

    private const string SourceSchemaArchiveFileName = "source-schema.zip";
    private const string ValidatedConfigurationFileName = "gateway.fgp";
    private const string CommittedConfigurationFileName = "gateway.far";
    private const string ArchiveContentType = "application/zip";

    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly GraphQLHttpClient _client;

    /// <summary>
    /// Initializes the Nitro API over an existing HTTP client.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client that sends the requests. It must not impose a timeout of its own, because
    /// a publication is observed over a long-running subscription.
    /// </param>
    /// <param name="disposeHttpClient">
    /// Specifies whether <paramref name="httpClient"/> is disposed with this instance.
    /// </param>
    public NitroFusionApi(HttpClient httpClient, bool disposeHttpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
        _disposeHttpClient = disposeHttpClient;
        _client = GraphQLHttpClient.Create(httpClient, disposeHttpClient: false);
    }

    /// <summary>
    /// Creates the Nitro API with an HTTP client that it owns.
    /// </summary>
    public static NitroFusionApi Create()
    {
        var httpClient = new HttpClient
        {
            // A publication is observed over a subscription that stays open for as long as Nitro
            // takes to reach a terminal state, so every bound comes from a cancellation token.
            Timeout = Timeout.InfiniteTimeSpan,
            DefaultRequestVersion = new Version(2, 0),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };

        return new NitroFusionApi(httpClient, disposeHttpClient: true);
    }

    /// <summary>
    /// Downloads an exact immutable source schema version, or returns <c>null</c> when the
    /// version does not exist. The caller owns a non-null result and must clear it after use.
    /// </summary>
    public async Task<byte[]?> DownloadSourceSchemaAsync(
        FusionTarget target,
        string name,
        string version,
        CancellationToken cancellationToken)
    {
        var requestUri = new Uri(
            target.CloudUrl,
            $"/api/v1/apis/{Uri.EscapeDataString(target.ApiId)}"
            + $"/fusion-subgraphs/{Uri.EscapeDataString(name)}"
            + $"/versions/{Uri.EscapeDataString(version)}/download");
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        NitroRequestHeaders.Apply(request, CreateCredential(target));

        using var response = await _httpClient.SendAsync(
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

    /// <summary>
    /// Uploads an immutable source schema version.
    /// </summary>
    public async Task<FusionRemoteCommandResult> UploadSourceSchemaAsync(
        FusionTarget target,
        string version,
        string archivePath,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["apiId"] = target.ApiId,
                ["tag"] = version,
                ["archive"] = new FileReference(
                    () => File.OpenRead(archivePath),
                    SourceSchemaArchiveFileName,
                    ArchiveContentType)
            }
        };

        var payload = await SendAsync(
            target,
            NitroOperationDocuments.UploadSourceSchemaOperationName,
            "uploadFusionSubgraph",
            variables,
            enableFileUploads: true,
            cancellationToken);
        var errors = GetErrors(payload);
        var hasVersion =
            payload.TryGetProperty("fusionSubgraphVersion", out var uploadedVersion)
            && uploadedVersion.ValueKind is JsonValueKind.Object;

        return new FusionRemoteCommandResult(
            hasVersion && errors.Count is 0,
            HasError(payload, "DuplicatedTagError"),
            errors);
    }

    /// <summary>
    /// Opens a publication request.
    /// </summary>
    public async Task<FusionRemoteBeginResult> BeginPublishAsync(
        FusionPublicationRequest request,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["apiId"] = request.Target.ApiId,
                ["stageName"] = request.Stage,
                ["tag"] = request.ConfigurationTag,
                ["waitForApproval"] = request.WaitForApproval,
                ["subgraphs"] = request.SourceSchemas
                    .Select(source => new Dictionary<string, object?>
                    {
                        ["name"] = source.Name,
                        ["tag"] = source.Version
                    })
                    .ToArray()
            }
        };

        var payload = await SendAsync(
            request.Target,
            NitroOperationDocuments.BeginDeploymentOperationName,
            "beginFusionConfigurationPublish",
            variables,
            enableFileUploads: false,
            cancellationToken);

        return new FusionRemoteBeginResult(
            GetString(payload, "requestId"),
            GetErrors(payload));
    }

    /// <summary>
    /// Observes the state of a publication request until Nitro ends the subscription.
    /// </summary>
    public async IAsyncEnumerable<FusionRemoteEvent> WatchPublishAsync(
        FusionTarget target,
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = CreateRequest(
            target,
            NitroOperationDocuments.WatchDeploymentOperationName,
            new Dictionary<string, object?> { ["requestId"] = requestId },
            enableFileUploads: false);

        var events = NitroGraphQL.StreamAsync(
            _client,
            request,
            ReadRemoteEvent,
            cancellationToken);

        await foreach (var remoteEvent in events.WithCancellation(cancellationToken))
        {
            yield return remoteEvent;
        }
    }

    /// <summary>
    /// Claims the publication slot of a publication request.
    /// </summary>
    public async Task<FusionRemoteCommandResult> ClaimPublishAsync(
        FusionTarget target,
        string requestId,
        CancellationToken cancellationToken)
        => ToCommandResult(
            await SendAsync(
                target,
                NitroOperationDocuments.ClaimDeploymentOperationName,
                "startFusionConfigurationComposition",
                CreateRequestIdVariables(requestId),
                enableFileUploads: false,
                cancellationToken));

    /// <summary>
    /// Releases the publication slot of a publication request.
    /// </summary>
    public async Task<FusionRemoteCommandResult> ReleasePublishAsync(
        FusionTarget target,
        string requestId,
        CancellationToken cancellationToken)
        => ToCommandResult(
            await SendAsync(
                target,
                NitroOperationDocuments.ReleaseDeploymentOperationName,
                "cancelFusionConfigurationComposition",
                CreateRequestIdVariables(requestId),
                enableFileUploads: false,
                cancellationToken));

    /// <summary>
    /// Hands the composed configuration to Nitro for validation.
    /// </summary>
    public async Task<FusionRemoteCommandResult> ValidatePublishAsync(
        FusionTarget target,
        string requestId,
        ReadOnlyMemory<byte> archive,
        CancellationToken cancellationToken)
        => ToCommandResult(
            await SendAsync(
                target,
                NitroOperationDocuments.ValidateDeploymentOperationName,
                "validateFusionConfigurationComposition",
                CreateConfigurationVariables(
                    requestId,
                    archive,
                    ValidatedConfigurationFileName),
                enableFileUploads: true,
                cancellationToken));

    /// <summary>
    /// Commits the composed configuration.
    /// </summary>
    public async Task<FusionRemoteCommandResult> CommitPublishAsync(
        FusionTarget target,
        string requestId,
        ReadOnlyMemory<byte> archive,
        CancellationToken cancellationToken)
        => ToCommandResult(
            await SendAsync(
                target,
                NitroOperationDocuments.CommitDeploymentOperationName,
                "commitFusionConfigurationPublish",
                CreateConfigurationVariables(
                    requestId,
                    archive,
                    CommittedConfigurationFileName),
                enableFileUploads: true,
                cancellationToken));

    /// <inheritdoc />
    public void Dispose()
    {
        _client.Dispose();

        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<JsonElement> SendAsync(
        FusionTarget target,
        string operationName,
        string fieldName,
        Dictionary<string, object?> variables,
        bool enableFileUploads,
        CancellationToken cancellationToken)
    {
        var result = await NitroGraphQL.SendWithRetryAsync(
            _client,
            CreateRequest(target, operationName, variables, enableFileUploads),
            // A publication operation is not safe to send more than once.
            retryTransientFailures: false,
            // The publication as a whole is bounded by the cancellation token of the caller.
            Timeout.InfiniteTimeSpan,
            cancellationToken);

        if (result.Failure is not null)
        {
            throw new FusionDeploymentException(result.Failure);
        }

        if (result.Data.ValueKind is not JsonValueKind.Object
            || !result.Data.TryGetProperty(fieldName, out var payload)
            || payload.ValueKind is not JsonValueKind.Object)
        {
            throw new FusionDeploymentException(
                "Nitro GraphQL request returned no data.");
        }

        return payload;
    }

    private static GraphQLHttpRequest CreateRequest(
        FusionTarget target,
        string operationName,
        Dictionary<string, object?> variables,
        bool enableFileUploads)
    {
        return new GraphQLHttpRequest(
            CreateBody(operationName, variables),
            NitroApiUrl.CreateGraphQLEndpoint(target.CloudUrl))
        {
            EnableFileUploads = enableFileUploads,
            OnMessageCreated = (_, requestMessage, _) =>
                NitroRequestHeaders.Apply(requestMessage, CreateCredential(target))
        };
    }

    private static OperationRequest CreateBody(
        string operationName,
        Dictionary<string, object?> variables)
    {
#if NITRO_PERSISTED_OPERATIONS
        var operationId = operationName switch
        {
            NitroOperationDocuments.UploadSourceSchemaOperationName
                => NitroOperationDocuments.GetUploadSourceSchemaOperationId(),
            NitroOperationDocuments.BeginDeploymentOperationName
                => NitroOperationDocuments.GetBeginDeploymentOperationId(),
            NitroOperationDocuments.ClaimDeploymentOperationName
                => NitroOperationDocuments.GetClaimDeploymentOperationId(),
            NitroOperationDocuments.ValidateDeploymentOperationName
                => NitroOperationDocuments.GetValidateDeploymentOperationId(),
            NitroOperationDocuments.CommitDeploymentOperationName
                => NitroOperationDocuments.GetCommitDeploymentOperationId(),
            NitroOperationDocuments.ReleaseDeploymentOperationName
                => NitroOperationDocuments.GetReleaseDeploymentOperationId(),
            NitroOperationDocuments.WatchDeploymentOperationName
                => NitroOperationDocuments.GetWatchDeploymentOperationId(),
            _ => throw UnknownOperation(operationName)
        };

        return new OperationRequest(
            id: operationId,
            operationName: operationName,
            variables: variables);
#else
        var document = operationName switch
        {
            NitroOperationDocuments.UploadSourceSchemaOperationName
                => NitroOperationDocuments.GetUploadSourceSchemaDocument(),
            NitroOperationDocuments.BeginDeploymentOperationName
                => NitroOperationDocuments.GetBeginDeploymentDocument(),
            NitroOperationDocuments.ClaimDeploymentOperationName
                => NitroOperationDocuments.GetClaimDeploymentDocument(),
            NitroOperationDocuments.ValidateDeploymentOperationName
                => NitroOperationDocuments.GetValidateDeploymentDocument(),
            NitroOperationDocuments.CommitDeploymentOperationName
                => NitroOperationDocuments.GetCommitDeploymentDocument(),
            NitroOperationDocuments.ReleaseDeploymentOperationName
                => NitroOperationDocuments.GetReleaseDeploymentDocument(),
            NitroOperationDocuments.WatchDeploymentOperationName
                => NitroOperationDocuments.GetWatchDeploymentDocument(),
            _ => throw UnknownOperation(operationName)
        };

        return new OperationRequest(
            document,
            operationName: operationName,
            variables: variables);
#endif
    }

    private static InvalidOperationException UnknownOperation(string operationName)
        => new($"The Nitro operation '{operationName}' is unknown.");

    private static Dictionary<string, object?> CreateRequestIdVariables(string requestId)
        => new()
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["requestId"] = requestId
            }
        };

    private static Dictionary<string, object?> CreateConfigurationVariables(
        string requestId,
        ReadOnlyMemory<byte> archive,
        string fileName)
        => new()
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["requestId"] = requestId,
                ["configuration"] = new FileReference(
                    () => OpenRead(archive),
                    fileName,
                    ArchiveContentType)
            }
        };

    private static NitroCredential CreateCredential(FusionTarget target)
        => NitroCredential.FromApiKey(target.ApiKey);

    private static FusionRemoteEvent ReadRemoteEvent(OperationResult result)
    {
        if (result.Data.ValueKind is not JsonValueKind.Object
            || !result.Data.TryGetProperty(
                "onFusionConfigurationPublishingTaskChanged",
                out var value)
            || value.ValueKind is not JsonValueKind.Object)
        {
            throw new FusionDeploymentException(
                "Nitro GraphQL request returned no data.");
        }

        var typeName = GetString(value, "__typename");
        var kind = typeName switch
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
        IReadOnlyList<string> errors = kind is FusionRemoteEventKind.PublishingFailed
            or FusionRemoteEventKind.ValidationFailed
                ? GetErrors(value)
                : [];

        return new FusionRemoteEvent(kind, errors);
    }

    private static FusionRemoteCommandResult ToCommandResult(JsonElement payload)
    {
        var errors = GetErrors(payload);

        return new FusionRemoteCommandResult(errors.Count is 0, false, errors);
    }

    private static List<string> GetErrors(JsonElement payload)
    {
        var messages = new List<string>();

        if (!payload.TryGetProperty("errors", out var errors)
            || errors.ValueKind is not JsonValueKind.Array)
        {
            return messages;
        }

        foreach (var error in errors.EnumerateArray())
        {
            if (GetString(error, "message") is { } message)
            {
                messages.Add(message);
            }
        }

        return messages;
    }

    private static bool HasError(JsonElement payload, string typeName)
    {
        if (!payload.TryGetProperty("errors", out var errors)
            || errors.ValueKind is not JsonValueKind.Array)
        {
            return false;
        }

        foreach (var error in errors.EnumerateArray())
        {
            if (string.Equals(GetString(error, "__typename"), typeName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetString(JsonElement value, string propertyName)
        => value.ValueKind is JsonValueKind.Object
            && value.TryGetProperty(propertyName, out var property)
            && property.ValueKind is JsonValueKind.String
                ? property.GetString()
                : null;

    private static Stream OpenRead(ReadOnlyMemory<byte> content)
    {
        if (MemoryMarshal.TryGetArray(content, out ArraySegment<byte> segment))
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

    private static FusionDeploymentException ArchiveResponseTooLarge(int maxArchiveSize)
        => new(
            "The compressed Fusion source schema archive exceeds the maximum "
            + $"allowed size of {maxArchiveSize} bytes.");
}

/// <summary>
/// The result of a Nitro operation that either succeeds or reports why it did not.
/// </summary>
internal sealed record FusionRemoteCommandResult(
    bool Succeeded,
    bool IsDuplicate,
    IReadOnlyList<string> Errors)
{
    public static FusionRemoteCommandResult Success { get; } = new(true, false, []);
}

/// <summary>
/// The result of opening a publication request.
/// </summary>
internal sealed record FusionRemoteBeginResult(
    string? RequestId,
    IReadOnlyList<string> Errors);

/// <summary>
/// A state change of a publication request.
/// </summary>
internal sealed record FusionRemoteEvent(
    FusionRemoteEventKind Kind,
    IReadOnlyList<string> Errors);

/// <summary>
/// The kind of state change that Nitro reported for a publication request.
/// </summary>
internal enum FusionRemoteEventKind
{
    Ready,
    Queued,
    ValidationSucceeded,
    ValidationFailed,
    PublishingSucceeded,
    PublishingFailed,
    InProgress,
    WaitingForApproval,
    Approved,
    Unknown
}
