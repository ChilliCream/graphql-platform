using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Properties;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// HTTP-based implementation of <see cref="ISourceSchemaClient"/> that sends GraphQL operations
/// to a downstream service over HTTP. Supports single requests, Apollo-style request batching,
/// and variable batching depending on the configured <see cref="SourceSchemaHttpClientConfiguration.Capabilities"/>.
/// </summary>
public sealed class SourceSchemaHttpClient : ISourceSchemaClient
{
    private static readonly Uri s_unknownUri = new("http://unknown");
    private static ReadOnlySpan<byte> VariableIndex => "variableIndex"u8;
    private static ReadOnlySpan<byte> RequestIndex => "requestIndex"u8;

    private readonly GraphQLHttpClient _client;
    private readonly SourceSchemaHttpClientConfiguration _configuration;
    private readonly bool _supportsVariableBatching;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="SourceSchemaHttpClient"/>.
    /// </summary>
    /// <param name="client">The underlying HTTP client used to send requests.</param>
    /// <param name="configuration">The transport configuration for this source schema.</param>
    public SourceSchemaHttpClient(
        GraphQLHttpClient client,
        SourceSchemaHttpClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configuration);

        _client = client;
        _configuration = configuration;

        var capabilities = configuration.Capabilities;

        Capabilities = capabilities;

        _supportsVariableBatching = capabilities.HasFlag(SourceSchemaClientCapabilities.VariableBatching);
    }

    /// <inheritdoc />
    public SourceSchemaClientCapabilities Capabilities { get; }

    /// <inheritdoc />
    public async ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        Debug.WriteLine(request.SchemaName);

        ChunkedArrayWriter? buffer = null;

        try
        {
            var httpRequest = CreateHttpRequest(context, request, ref buffer);
            ConfigureCallbacks(httpRequest, context, request.Node);

            var httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            return new Response(
                context,
                _configuration,
                _supportsVariableBatching,
                request.Node,
                request.OperationType,
                httpRequest.Uri ?? s_unknownUri,
                request.Variables,
                httpResponse,
                buffer);
        }
        catch
        {
            buffer?.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<BatchStreamResult> ExecuteBatchStreamAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfLessThan(requests.Length, 1, nameof(requests));

        if (ContainsSubscriptionRequest(requests))
        {
            throw new InvalidOperationException(
                FusionExecutionResources.SourceSchemaHttpClient_SubscriptionBatchNotSupported);
        }

        var requiresFileUpload = requests[0].RequiresFileUpload;
        ChunkedArrayWriter? buffer = null;

        try
        {
            var httpRequest = CreateHttpBatchRequest(
                context,
                requests,
                requiresFileUpload,
                ref buffer);
            ConfigureCallbacks(httpRequest, context, requests[0].Node);

            var results = ExecuteBatchStreamAsync(requests, httpRequest, cancellationToken);

            return _configuration.OnSourceSchemaResult is null
                ? results
                : WithBatchCallback(
                    results,
                    context,
                    requests,
                    _configuration.OnSourceSchemaResult,
                    cancellationToken);
        }
        catch
        {
            buffer?.Dispose();
            throw;
        }
    }

    private async IAsyncEnumerable<BatchStreamResult> ExecuteBatchStreamAsync(
        ImmutableArray<SourceSchemaClientRequest> requests,
        GraphQLHttpRequest httpRequest,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        bool? didFirstResultHaveRequestIndex = null;
        var currentRequestIndex = 0;
        var currentVariableIndex = 0;

        try
        {
            await foreach (var result in httpResponse.ReadAsResultStreamAsync().WithCancellation(cancellationToken))
            {
                // Check if the first result has a requestIndex.
                // If it does we can assume all others will have one as well and we know we need to check for it.
                // If it doesn't we can assume there won't be one and we can just simple counters.
                didFirstResultHaveRequestIndex ??= result.Root.TryGetProperty(RequestIndex, out _);

                // If we don't support variable batching, requests with multiple variables
                // might have been flattened to multiple requests.
                if (!_supportsVariableBatching)
                {
                    int requestIndex, variableIndex;

                    if (didFirstResultHaveRequestIndex.Value)
                    {
                        if (!result.Root.TryGetProperty(RequestIndex, out var flatRequestIndexElem))
                        {
                            throw new InvalidOperationException(
                                "Expected requestIndex in batch response but it was missing.");
                        }

                        ResolveFlattenedIndex(requests, flatRequestIndexElem.GetInt32(), out requestIndex, out variableIndex);
                    }
                    else
                    {
                        requestIndex = currentRequestIndex;
                        variableIndex = currentVariableIndex;

                        if (++currentVariableIndex >= Math.Max(1, requests[currentRequestIndex].Variables.Length))
                        {
                            currentRequestIndex++;
                            currentVariableIndex = 0;
                        }
                    }

                    if (TryGetResultPath(requests[requestIndex], variableIndex, out var p, out var ap))
                    {
                        var ssr = ap.IsDefaultOrEmpty
                            ? new SourceSchemaResult(p, result)
                            : new SourceSchemaResult(p, result, additionalPaths: ap);

                        yield return new BatchStreamResult(requestIndex, ssr);
                    }
                }
                else
                {
                    var requestIndex = ResolveRequestIndex(requests, result);

                    if (requestIndex == -1)
                    {
                        for (var i = 0; i < requests.Length; i++)
                        {
                            var req = requests[i];

                            if (!TryGetResultPath(req, variableIndex: 0, out var p, out var ap))
                            {
                                continue;
                            }

                            var ssr = ap.IsDefaultOrEmpty
                                ? new SourceSchemaResult(p, result)
                                : new SourceSchemaResult(p, result, additionalPaths: ap);

                            yield return new BatchStreamResult(i, ssr);
                        }

                        continue;
                    }

                    var request = requests[requestIndex];
                    var variableIndex = ResolveVariableIndex(request, result);

                    if (variableIndex == -1)
                    {
                        for (var vi = 0; vi < request.Variables.Length; vi++)
                        {
                            if (!TryGetResultPath(request, vi, out var vp, out var vap))
                            {
                                continue;
                            }

                            var vssr = vap.IsDefaultOrEmpty
                                ? new SourceSchemaResult(vp, result)
                                : new SourceSchemaResult(vp, result, additionalPaths: vap);

                            yield return new BatchStreamResult(requestIndex, vssr);
                        }

                        continue;
                    }

                    if (!TryGetResultPath(request, variableIndex, out var path, out var additionalPaths))
                    {
                        result.Dispose();
                        throw new InvalidOperationException(
                            string.Format(
                                FusionExecutionResources.SourceSchemaHttpClient_InvalidVariableIndex,
                                variableIndex,
                                request.Node.Id));
                    }

                    var sourceSchemaResult = additionalPaths.IsDefaultOrEmpty
                        ? new SourceSchemaResult(path, result)
                        : new SourceSchemaResult(path, result, additionalPaths: additionalPaths);

                    yield return new BatchStreamResult(requestIndex, sourceSchemaResult);
                }
            }
        }
        finally
        {
            httpResponse.Dispose();
        }
    }

    private static async IAsyncEnumerable<BatchStreamResult> WithBatchCallback(
        IAsyncEnumerable<BatchStreamResult> results,
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        Action<OperationPlanContext, ExecutionNode, SourceSchemaResult> onSourceSchemaResult,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var batchResult in results.WithCancellation(cancellationToken))
        {
            onSourceSchemaResult(context, requests[batchResult.RequestIndex].Node, batchResult.Result);
            yield return batchResult;
        }
    }

    /// <summary>
    /// Creates the appropriate <see cref="GraphQLHttpRequest"/> for the given request,
    /// choosing between a single operation, an Apollo operation batch, or a variable batch
    /// based on the number of variable sets and the configured batching mode.
    /// </summary>
    private GraphQLHttpRequest CreateHttpRequest(
        OperationPlanContext context,
        SourceSchemaClientRequest originalRequest,
        ref ChunkedArrayWriter? buffer)
    {
        var defaultAcceptHeader = originalRequest.OperationType is OperationType.Subscription
            ? _configuration.SubscriptionAcceptHeaderValue
            : _configuration.DefaultAcceptHeaderValue;
        var operationSourceText = originalRequest.OperationSourceText;

        GraphQLHttpRequest httpRequest;

        switch (originalRequest.Variables.Length)
        {
            case 0:
                httpRequest = new GraphQLHttpRequest(CreateSingleRequest(context, originalRequest, ref buffer))
                {
                    Uri = _configuration.BaseAddress, AcceptHeaderValue = defaultAcceptHeader
                };
                break;

            case 1:
                httpRequest = new GraphQLHttpRequest(CreateSingleRequest(context, originalRequest, ref buffer))
                {
                    Uri = _configuration.BaseAddress,
                    AcceptHeaderValue = defaultAcceptHeader,
                    EnableFileUploads = originalRequest.RequiresFileUpload
                };
                break;

            default:
                if (!originalRequest.RequiresFileUpload && _supportsVariableBatching)
                {
                    httpRequest =
                        new GraphQLHttpRequest(CreateVariableBatchRequest(operationSourceText, originalRequest))
                        {
                            Uri = _configuration.BaseAddress,
                            AcceptHeaderValue = _configuration.BatchingAcceptHeaderValue
                        };
                }
                else
                {
                    httpRequest =
                        new GraphQLHttpRequest(CreateOperationBatchRequest(context, originalRequest, ref buffer))
                        {
                            Uri = _configuration.BaseAddress,
                            AcceptHeaderValue = _configuration.BatchingAcceptHeaderValue,
                            EnableFileUploads = originalRequest.RequiresFileUpload
                        };
                }

                break;
        }

        httpRequest.OperationKind = originalRequest.OperationType;
        return httpRequest;
    }

    private GraphQLHttpRequest CreateHttpBatchRequest(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> originalRequests,
        bool requiresFileUpload,
        ref ChunkedArrayWriter? buffer)
    {
        if (requiresFileUpload
            && context.RequestContext.Features.Get<IFileLookup>() is { } fileLookup)
        {
            var capacity = originalRequests.Length;

            foreach (var sourceRequest in originalRequests)
            {
                if (sourceRequest.Variables.Length > 1)
                {
                    capacity += sourceRequest.Variables.Length - 1;
                }
            }

            var batchRequests = ImmutableArray.CreateBuilder<IOperationRequest>(capacity);
            var fileEntries = ImmutableArray.CreateBuilder<FileEntry>();
            buffer ??= new ChunkedArrayWriter();
            var i = 0;

            foreach (var sourceRequest in originalRequests)
            {
                switch (sourceRequest.Variables.Length)
                {
                    case 0:
                        batchRequests.Add(
                            CreateBatchUploadRequest(
                                sourceRequest,
                                VariableValues.Empty,
                                buffer,
                                fileLookup,
                                fileEntries));
                        i++;
                        break;

                    case 1:
                        batchRequests.Add(
                            CreateBatchUploadRequest(
                                sourceRequest,
                                sourceRequest.Variables[0],
                                buffer,
                                fileLookup,
                                fileEntries,
                                $"{i}.variables"));
                        i++;
                        break;

                    default:
                        for (var j = 0; j < sourceRequest.Variables.Length; j++)
                        {
                            batchRequests.Add(
                                CreateBatchUploadRequest(
                                    sourceRequest,
                                    sourceRequest.Variables[j],
                                    buffer,
                                    fileLookup,
                                    fileEntries,
                                    $"{i}.variables"));
                            i++;
                        }

                        break;
                }
            }

            return new GraphQLHttpRequest(
                new OperationBatchRequest(batchRequests.MoveToImmutable(), fileEntries.ToImmutable()))
            {
                Uri = _configuration.BaseAddress,
                AcceptHeaderValue = _configuration.BatchingAcceptHeaderValue,
                EnableFileUploads = true,
                OperationKind = originalRequests[0].OperationType
            };
        }
        else
        {
            var capacity = originalRequests.Length;

            if (!_supportsVariableBatching)
            {
                foreach (var sourceRequest in originalRequests)
                {
                    if (sourceRequest.Variables.Length > 1)
                    {
                        capacity += sourceRequest.Variables.Length - 1;
                    }
                }
            }

            var batchRequests = ImmutableArray.CreateBuilder<IOperationRequest>(capacity);

            foreach (var sourceRequest in originalRequests)
            {
                switch (sourceRequest.Variables.Length)
                {
                    case 0 or 1:
                        batchRequests.Add(CreateSingleRequest(context, sourceRequest, ref buffer));
                        break;

                    default:
                        if (_supportsVariableBatching)
                        {
                            batchRequests.Add(CreateVariableBatchRequest(
                                sourceRequest.OperationSourceText, sourceRequest));
                        }
                        else
                        {
                            for (var j = 0; j < sourceRequest.Variables.Length; j++)
                            {
                                batchRequests.Add(new OperationRequest(
                                    sourceRequest.OperationSourceText,
                                    id: null,
                                    operationName: null,
                                    onError: null,
                                    variables: sourceRequest.Variables[j],
                                    extensions: JsonSegment.Empty));
                            }
                        }

                        break;
                }
            }

            return new GraphQLHttpRequest(new OperationBatchRequest(batchRequests.MoveToImmutable()))
            {
                Uri = _configuration.BaseAddress,
                AcceptHeaderValue = _configuration.BatchingAcceptHeaderValue,
                OperationKind = originalRequests[0].OperationType
            };
        }
    }

    private static OperationRequest CreateSingleRequest(
        OperationPlanContext context,
        SourceSchemaClientRequest originalRequest,
        ref ChunkedArrayWriter? writer)
    {
        var variables = originalRequest.Variables.IsDefaultOrEmpty
            ? VariableValues.Empty
            : originalRequest.Variables[0];

        if (originalRequest.RequiresFileUpload
            && context.RequestContext.Features.Get<IFileLookup>() is { } fileLookup)
        {
            writer ??= new ChunkedArrayWriter();
            var (cleanedJson, fileMap) = FileEntryBuilder.Build(writer, variables.Values, fileLookup);

            return new OperationRequest(
                originalRequest.OperationSourceText,
                id: null,
                operationName: null,
                onError: null,
                variables: variables with { Values = cleanedJson },
                extensions: JsonSegment.Empty,
                fileMap: fileMap);
        }

        return new OperationRequest(
            originalRequest.OperationSourceText,
            id: null,
            operationName: null,
            onError: null,
            variables: variables,
            extensions: JsonSegment.Empty);
    }

    private static OperationRequest CreateBatchUploadRequest(
        SourceSchemaClientRequest originalRequest,
        VariableValues variables,
        ChunkedArrayWriter writer,
        IFileLookup fileLookup,
        ImmutableArray<FileEntry>.Builder fileEntries,
        string pathPrefix = "variables")
    {
        var cleanedJson = FileEntryBuilder.Build(writer, variables.Values, fileLookup, fileEntries, pathPrefix);

        return new OperationRequest(
            originalRequest.OperationSourceText,
            id: null,
            operationName: null,
            onError: null,
            variables: variables with { Values = cleanedJson },
            extensions: JsonSegment.Empty);
    }

    private static OperationBatchRequest CreateOperationBatchRequest(
        OperationPlanContext context,
        SourceSchemaClientRequest originalRequest,
        ref ChunkedArrayWriter? writer)
    {
        if (originalRequest.RequiresFileUpload
            && context.RequestContext.Features.Get<IFileLookup>() is { } fileLookup)
        {
            writer ??= new ChunkedArrayWriter();
            var fileEntries = ImmutableArray.CreateBuilder<FileEntry>();
            var requests = new OperationRequest[originalRequest.Variables.Length];

            for (var i = 0; i < requests.Length; i++)
            {
                requests[i] = CreateBatchUploadRequest(
                    originalRequest,
                    originalRequest.Variables[i],
                    writer,
                    fileLookup,
                    fileEntries,
                    $"{i}.variables");
            }

            return new OperationBatchRequest(
                ImmutableArray.Create<IOperationRequest>(requests),
                fileEntries.ToImmutable());
        }
        else
        {
            var requests = new OperationRequest[originalRequest.Variables.Length];

            for (var i = 0; i < requests.Length; i++)
            {
                requests[i] = new OperationRequest(
                    originalRequest.OperationSourceText,
                    id: null,
                    operationName: null,
                    onError: null,
                    variables: originalRequest.Variables[i],
                    extensions: JsonSegment.Empty);
            }

            return new OperationBatchRequest(ImmutableArray.Create<IOperationRequest>(requests));
        }
    }

    private static VariableBatchRequest CreateVariableBatchRequest(
        string operationSourceText,
        SourceSchemaClientRequest originalRequest)
    {
        return new VariableBatchRequest(
            operationSourceText,
            id: null,
            operationName: null,
            onError: null,
            variables: originalRequest.Variables,
            extensions: JsonSegment.Empty);
    }

    private static int ResolveRequestIndex(
        ImmutableArray<SourceSchemaClientRequest> requests,
        SourceResultDocument result)
    {
        if (requests.Length == 1)
        {
            return 0;
        }

        if (!result.Root.TryGetProperty(RequestIndex, out var requestIndexElement))
        {
            return -1;
        }

        var requestIndex = requestIndexElement.GetInt32();

        if ((uint)requestIndex < (uint)requests.Length)
        {
            return requestIndex;
        }

        throw ThrowHelper.RequestIndexOutOfRange(requestIndex);
    }

    private static int ResolveVariableIndex(
        SourceSchemaClientRequest request,
        SourceResultDocument result)
    {
        var variableCount = request.Variables.Length;

        if (variableCount <= 1)
        {
            return 0;
        }

        if (!result.Root.TryGetProperty(VariableIndex, out var variableIndexElement))
        {
            return -1;
        }

        var variableIndex = variableIndexElement.GetInt32();

        if ((uint)variableIndex < (uint)variableCount)
        {
            return variableIndex;
        }

        throw ThrowHelper.VariableIndexOutOfRange(variableIndex);
    }

    private static void ResolveFlattenedIndex(
        ImmutableArray<SourceSchemaClientRequest> requests,
        int flattenedIndex,
        out int requestIndex,
        out int variableIndex)
    {
        var remaining = flattenedIndex;

        for (var i = 0; i < requests.Length; i++)
        {
            var varCount = Math.Max(1, requests[i].Variables.Length);

            if (remaining < varCount)
            {
                requestIndex = i;
                variableIndex = remaining;
                return;
            }

            remaining -= varCount;
        }

        throw new InvalidOperationException(
            $"Flattened index {flattenedIndex} is out of range.");
    }

    private static bool TryGetResultPath(
        SourceSchemaClientRequest request,
        int variableIndex,
        out CompactPath path,
        out CompactPathSegment additionalPaths)
    {
        if (request.Variables.Length == 0)
        {
            path = CompactPath.Root;
            additionalPaths = default;
            return true;
        }

        if ((uint)variableIndex >= (uint)request.Variables.Length)
        {
            path = CompactPath.Root;
            additionalPaths = default;
            return false;
        }

        var variable = request.Variables[variableIndex];
        path = variable.Path;
        additionalPaths = variable.AdditionalPaths;
        return true;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _client.Dispose();
        _disposed = true;

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Attaches <see cref="SourceSchemaHttpClientConfiguration.OnBeforeSend"/> and
    /// <see cref="SourceSchemaHttpClientConfiguration.OnAfterReceive"/> callbacks to
    /// the HTTP request.
    /// </summary>
    private void ConfigureCallbacks(
        GraphQLHttpRequest request,
        OperationPlanContext context,
        ExecutionNode node)
    {
        if (_configuration.OnBeforeSend is null && _configuration.OnAfterReceive is null)
        {
            return;
        }

        request.State = new RequestCallbackState(context, node, _configuration);

        if (_configuration.OnBeforeSend is not null)
        {
            request.OnMessageCreated += static (_, requestMessage, state) =>
                state.Configuration.OnBeforeSend!.Invoke(state.Context, state.Node, requestMessage);
        }

        if (_configuration.OnAfterReceive is not null)
        {
            request.OnMessageReceived += static (_, responseMessage, state) =>
                state.Configuration.OnAfterReceive!.Invoke(state.Context, state.Node, responseMessage);
        }
    }

    private static bool ContainsSubscriptionRequest(
        IReadOnlyList<SourceSchemaClientRequest> requests)
    {
        for (var i = 0; i < requests.Count; i++)
        {
            if (requests[i].OperationType is OperationType.Subscription)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A live response backed by an in-flight HTTP response. Used for single (non-batched)
    /// requests where the response stream is read lazily on enumeration.
    /// </summary>
    private sealed class Response(
        OperationPlanContext context,
        SourceSchemaHttpClientConfiguration configuration,
        bool supportsVariableBatching,
        ExecutionNode node,
        OperationType operation,
        Uri uri,
        ImmutableArray<VariableValues> variables,
        GraphQLHttpResponse response,
        ChunkedArrayWriter? buffer)
        : SourceSchemaClientResponse
    {
        public override Uri Uri => uri;

        public override string ContentType => response.RawContentType ?? "unknown";

        public override bool IsSuccessful => response.IsSuccessStatusCode;

        public override IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
            CancellationToken cancellationToken = default)
        {
            var results = ReadAsResultStreamCoreAsync(cancellationToken);

            return configuration.OnSourceSchemaResult is null
                ? results
                : WithResultCallback(
                    results,
                    context,
                    node,
                    configuration.OnSourceSchemaResult,
                    cancellationToken);
        }

        private async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamCoreAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (operation is OperationType.Subscription)
            {
                await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cancellationToken))
                {
                    yield return new SourceSchemaResult(CompactPath.Root, result);
                }
            }
            else
            {
                switch (variables.Length)
                {
                    case 0:
                    {
                        var result = await response.ReadAsResultAsync(cancellationToken);
                        yield return new SourceSchemaResult(CompactPath.Root, result);
                        break;
                    }

                    case 1:
                    {
                        var result = await response.ReadAsResultAsync(cancellationToken);
                        var variable = variables[0];
                        yield return new SourceSchemaResult(
                            variable.Path,
                            result,
                            additionalPaths: variable.AdditionalPaths);
                        break;
                    }

                    default:
                    {
                        SourceSchemaResult? errorResult = null;

                        if (supportsVariableBatching)
                        {
                            await foreach (var result in response.ReadAsResultStreamAsync()
                                .WithCancellation(cancellationToken))
                            {
                                if (!result.Root.TryGetProperty(VariableIndex, out var variableIndex)
                                    || variableIndex.ValueKind is not JsonValueKind.Number)
                                {
                                    // if we do not get a variable index we have a protocol error
                                    // and must terminate the request.
                                    errorResult = new SourceSchemaResult(variables[0].Path, result);
                                    break;
                                }

                                var index = variableIndex.GetInt32();
                                if ((uint)index >= (uint)variables.Length)
                                {
                                    // if the variable index is larger than the amount of variable sets we have
                                    // we also have a protocol issue and must terminate the request.
                                    errorResult = new SourceSchemaResult(variables[0].Path, result);
                                    break;
                                }

                                var variable = variables[index];
                                yield return new SourceSchemaResult(
                                    variable.Path, result, additionalPaths: variable.AdditionalPaths);
                            }
                        }
                        else
                        {
                            var requestIndex = 0;
                            await foreach (var result in response.ReadAsResultStreamAsync()
                                .WithCancellation(cancellationToken))
                            {
                                if ((uint)requestIndex >= (uint)variables.Length)
                                {
                                    errorResult = new SourceSchemaResult(variables[0].Path, result);
                                    break;
                                }

                                var variable = variables[requestIndex];
                                yield return new SourceSchemaResult(
                                    variable.Path, result, additionalPaths: variable.AdditionalPaths);

                                requestIndex++;
                            }
                        }

                        if (errorResult is not null)
                        {
                            for (var i = 0; i < variables.Length; i++)
                            {
                                var variable = variables[i];
                                yield return errorResult.WithPath(variable.Path, variable.AdditionalPaths);
                            }
                        }

                        break;
                    }
                }
            }
        }

        private static async IAsyncEnumerable<SourceSchemaResult> WithResultCallback(
            IAsyncEnumerable<SourceSchemaResult> results,
            OperationPlanContext context,
            ExecutionNode node,
            Action<OperationPlanContext, ExecutionNode, SourceSchemaResult> onSourceSchemaResult,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var sourceSchemaResult in results.WithCancellation(cancellationToken))
            {
                onSourceSchemaResult(context, node, sourceSchemaResult);
                yield return sourceSchemaResult;
            }
        }

        public override void Dispose()
        {
            response.Dispose();
            buffer?.Dispose();
        }
    }
}
