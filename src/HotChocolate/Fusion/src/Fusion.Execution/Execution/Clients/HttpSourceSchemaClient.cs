using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Properties;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// HTTP-based implementation of <see cref="ISourceSchemaClient"/> that sends GraphQL operations
/// to a downstream service over HTTP. Supports single requests, Apollo-style request batching,
/// and variable batching depending on the configured <see cref="HttpSourceSchemaClientConfiguration.Capabilities"/>.
/// </summary>
public sealed partial class HttpSourceSchemaClient : ISourceSchemaClient
{
    private readonly GraphQLHttpClient _client;
    private readonly HttpSourceSchemaClientConfiguration _configuration;
    private readonly ErrorHandlingMode? _onError;
    private readonly bool _supportsVariableBatching;
    private readonly bool _useAliasBatchingForVariableSets;
    private readonly bool _useAliasBatchingForOperationBatches;
    private readonly bool _annotateOperationKind;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpSourceSchemaClient"/>.
    /// </summary>
    /// <param name="client">The underlying HTTP client used to send requests.</param>
    /// <param name="configuration">The transport configuration for this source schema.</param>
    /// <param name="annotateOperationKind">
    /// Whether the GraphQL operation kind is annotated onto outgoing subgraph HTTP requests via
    /// <see cref="HttpRequestMessage.Options"/>, so that delegating handlers (such as the
    /// RequestDeduplicationHandler) can consume it. <c>false</c> by default because materializing the
    /// request options bag allocates per request.
    /// </param>
    public HttpSourceSchemaClient(
        GraphQLHttpClient client,
        HttpSourceSchemaClientConfiguration configuration,
        bool annotateOperationKind = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configuration);

        _client = client;
        _configuration = configuration;
        _onError = configuration.OnError;
        _annotateOperationKind = annotateOperationKind;

        var capabilities = configuration.Capabilities;

        Capabilities = capabilities;

        _supportsVariableBatching = capabilities.HasFlag(SourceSchemaClientCapabilities.VariableBatching);

        var supportsAliasBatching = capabilities.HasFlag(SourceSchemaClientCapabilities.AliasBatching);
        var supportsRequestBatching = capabilities.HasFlag(SourceSchemaClientCapabilities.RequestBatching);

        // The many variable sets of one operation are merged into one aliased operation only when
        // the source schema does not support variable batching.
        _useAliasBatchingForVariableSets = supportsAliasBatching && !_supportsVariableBatching;

        // The distinct operations of one batch are merged into one aliased operation only when the
        // source schema does not support request batching.
        _useAliasBatchingForOperationBatches = supportsAliasBatching && !supportsRequestBatching;
    }

    /// <inheritdoc />
    public SourceSchemaClientCapabilities Capabilities { get; }

    /// <inheritdoc />
    public IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (request.OperationType is OperationType.Subscription)
        {
            throw new InvalidOperationException(
                FusionExecutionResources.SourceSchemaClient_SubscriptionsNotSupportedByExecute);
        }

        Debug.WriteLine(request.SchemaName);

        if (_useAliasBatchingForVariableSets
            && request.Variables.Length > 1
            && CanAliasBatch(request, out _, out _))
        {
            return ExecuteAliasBatchAsync(context, request, cancellationToken);
        }

        return CreateStreamAsync(context, request, cancellationToken);

        async IAsyncEnumerable<SourceSchemaResult> CreateStreamAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ChunkedArrayWriter? buffer = null;
            GraphQLHttpResponse? httpResponse = null;

            try
            {
                var httpRequest = CreateHttpRequest(context, in request, ref buffer);
                SourceSchemaCallbackHelper.ConfigureCallbacks(
                    httpRequest,
                    context,
                    request.Node,
                    _configuration);

                httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
                context.TrackTransport(request.Node, httpRequest.Uri, httpResponse.RawContentType);

                var results = ReadAsResultStreamAsync(
                    context,
                    request.Variables,
                    httpResponse,
                    cancellationToken);

                if (_configuration.OnSourceSchemaResult is not null)
                {
                    results = WithResultCallback(
                        results,
                        context,
                        request.Node,
                        _configuration.OnSourceSchemaResult,
                        cancellationToken);
                }

                await foreach (var result in results.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return result;
                }
            }
            finally
            {
                httpResponse?.Dispose();
                buffer?.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfLessThan(requests.Length, 1, nameof(requests));

        if (ContainsSubscriptionRequest(requests))
        {
            throw new InvalidOperationException(
                FusionExecutionResources.HttpSourceSchemaClient_SubscriptionBatchNotSupported);
        }

        if (_useAliasBatchingForOperationBatches)
        {
            return ExecuteAliasBatchAsync(context, requests, cancellationToken);
        }

        return ExecuteBatchWithNativeProtocolAsync(context, requests, cancellationToken);
    }

    /// <summary>
    /// Sends a batch through the protocol extensions that the source schema supports, which is
    /// either an operation batch or one request per operation.
    /// </summary>
    private IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchWithNativeProtocolAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        CancellationToken cancellationToken)
    {
        // A source schema that does not support request batching still accepts
        // each operation on its own, so we fall back to individual requests and
        // stream the results back in request order.
        if (!Capabilities.HasFlag(SourceSchemaClientCapabilities.RequestBatching))
        {
            return ExecuteBatchAsSingleRequestsAsync(context, requests, cancellationToken);
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
            SourceSchemaCallbackHelper.ConfigureCallbacks(httpRequest, context, requests[0].Node, _configuration);

            var results = ExecuteBatchStreamAsync(context, requests, httpRequest, buffer, cancellationToken);

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

    /// <inheritdoc />
    public async IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Each event is read through the active memory source, which hands out a fresh arena for
        // every event so that each event document is backed by its own arena.
        var arenaSource = context.MemorySource;
        ChunkedArrayWriter? buffer = null;
        GraphQLHttpResponse? httpResponse = null;

        try
        {
            var httpRequest = CreateHttpRequest(context, in request, ref buffer);
            SourceSchemaCallbackHelper.ConfigureCallbacks(httpRequest, context, request.Node, _configuration);

            httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            context.TrackTransport(request.Node, httpRequest.Uri, httpResponse.RawContentType);

            // The read timeout bounds the wait for the next bytes of the event stream, keep-alives included.
            var documents = httpResponse.ReadAsResultStreamAsync(
                arenaSource,
                requireStreaming: true,
                _configuration.SubscriptionReadTimeout);

            await foreach (var document in documents.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var result = new SourceSchemaResult(CompactPath.Root, document);
                _configuration.OnSourceSchemaResult?.Invoke(context, request.Node, result);
                yield return result;
            }
        }
        finally
        {
            httpResponse?.Dispose();
            buffer?.Dispose();
        }
    }

    private async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsSingleRequestsAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Each request is its own HTTP round-trip, so a transport failure of one
        // request must not abort the batch. We isolate each request: if it fails
        // we record the cause against its index and continue with the next one,
        // leaving results already produced by sibling requests untouched.
        for (var i = 0; i < requests.Length; i++)
        {
            await foreach (var result in ExecuteIsolatedAsync(context, requests[i], i, cancellationToken)
                .WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Executes a single request of a batch as its own round-trip. A transport failure of one
    /// request must not abort the batch, so its cause is recorded against the request's index and
    /// the results already produced by sibling requests stay untouched.
    /// </summary>
    private async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteIsolatedAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        int requestIndex,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enumerator = ExecuteAsync(context, request, cancellationToken)
            .WithCancellation(cancellationToken).ConfigureAwait(false).GetAsyncEnumerator();

        try
        {
            while (true)
            {
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Caller-driven cancellation aborts the whole batch.
                    throw;
                }
                catch (Exception exception)
                {
                    // The consuming node attaches the recorded cause to the
                    // request that produced no result.
                    context.TrackBatchRequestError(request.Node, requestIndex, exception);
                    break;
                }

                yield return new SourceSchemaBatchResult(requestIndex, enumerator.Current);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    internal async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchStreamAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        GraphQLHttpRequest httpRequest,
        ChunkedArrayWriter? buffer,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var arenaSource = context.MemorySource;
        GraphQLHttpResponse? httpResponse = null;

        try
        {
            httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            context.TrackTransport(requests.ItemRef(0).Node, httpRequest.Uri, httpResponse.RawContentType);

            var mapper = new BatchResultMapper(requests, _supportsVariableBatching);

            await foreach (var result in httpResponse.ReadAsResultStreamAsync(arenaSource).WithCancellation(cancellationToken))
            {
                mapper.Begin(result);

                while (mapper.TryGetNext(out var requestIndex, out var sourceSchemaResult))
                {
                    yield return new SourceSchemaBatchResult(requestIndex, sourceSchemaResult);
                }
            }
        }
        finally
        {
            httpResponse?.Dispose();
            buffer?.Dispose();
        }
    }

    private static async IAsyncEnumerable<SourceSchemaBatchResult> WithBatchCallback(
        IAsyncEnumerable<SourceSchemaBatchResult> results,
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        Action<OperationPlanContext, ExecutionNode, SourceSchemaResult> onSourceSchemaResult,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var batchResult in results.WithCancellation(cancellationToken))
        {
            onSourceSchemaResult(context, requests.ItemRef(batchResult.RequestIndex).Node, batchResult.Result);
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
        in SourceSchemaClientRequest originalRequest,
        ref ChunkedArrayWriter? buffer)
    {
        var defaultAcceptHeader = originalRequest.OperationType is OperationType.Subscription
            ? _configuration.SubscriptionAcceptHeaderValue
            : _configuration.DefaultAcceptHeaderValue;
        var operationSourceText = originalRequest.OperationSourceText.Value;

        GraphQLHttpRequest httpRequest;

        switch (originalRequest.Variables.Length)
        {
            case 0:
                httpRequest = new GraphQLHttpRequest(CreateSingleRequest(context, in originalRequest, _onError, ref buffer))
                {
                    Uri = _configuration.BaseAddress,
                    AcceptHeaderValue = defaultAcceptHeader
                };
                break;

            case 1:
                httpRequest = new GraphQLHttpRequest(CreateSingleRequest(context, in originalRequest, _onError, ref buffer))
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
                        new GraphQLHttpRequest(CreateVariableBatchRequest(operationSourceText, in originalRequest, _onError))
                        {
                            Uri = _configuration.BaseAddress,
                            AcceptHeaderValue = _configuration.BatchingAcceptHeaderValue
                        };
                }
                else
                {
                    httpRequest =
                        new GraphQLHttpRequest(CreateOperationBatchRequest(context, in originalRequest, _onError, ref buffer))
                        {
                            Uri = _configuration.BaseAddress,
                            AcceptHeaderValue = _configuration.BatchingAcceptHeaderValue,
                            EnableFileUploads = originalRequest.RequiresFileUpload
                        };
                }

                break;
        }

        httpRequest.OperationKind = GetOperationKindHint(originalRequest.OperationType);
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
                                _onError,
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
                                _onError,
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
                                    _onError,
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
                OperationKind = GetOperationKindHint(originalRequests[0].OperationType)
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
                        batchRequests.Add(CreateSingleRequest(context, sourceRequest, _onError, ref buffer));
                        break;

                    default:
                        if (_supportsVariableBatching)
                        {
                            batchRequests.Add(CreateVariableBatchRequest(
                                sourceRequest.OperationSourceText.Value, sourceRequest, _onError));
                        }
                        else
                        {
                            for (var j = 0; j < sourceRequest.Variables.Length; j++)
                            {
                                batchRequests.Add(new OperationRequest(
                                    sourceRequest.OperationSourceText.Value,
                                    id: null,
                                    operationName: null,
                                    onError: _onError,
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
                OperationKind = GetOperationKindHint(originalRequests[0].OperationType)
            };
        }
    }

    private OperationType? GetOperationKindHint(OperationType operationType)
        => _annotateOperationKind ? operationType : null;

    private static OperationRequest CreateSingleRequest(
        OperationPlanContext context,
        in SourceSchemaClientRequest originalRequest,
        ErrorHandlingMode? onError,
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
                originalRequest.OperationSourceText.Value,
                id: null,
                operationName: null,
                onError: onError,
                variables: variables with { Values = cleanedJson },
                extensions: JsonSegment.Empty,
                fileMap: fileMap);
        }

        return new OperationRequest(
            originalRequest.OperationSourceText.Value,
            id: null,
            operationName: null,
            onError: onError,
            variables: variables,
            extensions: JsonSegment.Empty);
    }

    private static OperationRequest CreateBatchUploadRequest(
        in SourceSchemaClientRequest originalRequest,
        VariableValues variables,
        ErrorHandlingMode? onError,
        ChunkedArrayWriter writer,
        IFileLookup fileLookup,
        ImmutableArray<FileEntry>.Builder fileEntries,
        string pathPrefix = "variables")
    {
        var cleanedJson = FileEntryBuilder.Build(writer, variables.Values, fileLookup, fileEntries, pathPrefix);

        return new OperationRequest(
            originalRequest.OperationSourceText.Value,
            id: null,
            operationName: null,
            onError: onError,
            variables: variables with { Values = cleanedJson },
            extensions: JsonSegment.Empty);
    }

    private static OperationBatchRequest CreateOperationBatchRequest(
        OperationPlanContext context,
        in SourceSchemaClientRequest originalRequest,
        ErrorHandlingMode? onError,
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
                    onError,
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
                    originalRequest.OperationSourceText.Value,
                    id: null,
                    operationName: null,
                    onError: onError,
                    variables: originalRequest.Variables[i],
                    extensions: JsonSegment.Empty);
            }

            return new OperationBatchRequest(ImmutableArray.Create<IOperationRequest>(requests));
        }
    }

    private static VariableBatchRequest CreateVariableBatchRequest(
        ReadOnlyMemory<byte> operationSourceText,
        in SourceSchemaClientRequest originalRequest,
        ErrorHandlingMode? onError)
    {
        return new VariableBatchRequest(
            operationSourceText,
            id: null,
            operationName: null,
            onError: onError,
            variables: originalRequest.Variables,
            extensions: JsonSegment.Empty);
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

    private static bool ContainsSubscriptionRequest(
        ImmutableArray<SourceSchemaClientRequest> requests)
    {
        for (var i = 0; i < requests.Length; i++)
        {
            if (requests.ItemRef(i).OperationType is OperationType.Subscription)
            {
                return true;
            }
        }

        return false;
    }

    private async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
        OperationPlanContext context,
        ImmutableArray<VariableValues> variables,
        GraphQLHttpResponse response,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (variables.Length <= 1)
        {
            var document = await response.ReadAsResultAsync(context.Memory, cancellationToken);
            yield return CreateSingleResult(variables, document);
            yield break;
        }

        var mapper = new VariableBatchResultMapper(variables, _supportsVariableBatching);

        await foreach (var document in response.ReadAsResultStreamAsync(context.MemorySource)
            .WithCancellation(cancellationToken))
        {
            if (!mapper.TryMap(document, out var result))
            {
                break;
            }

            yield return result;
        }

        for (var i = 0; i < mapper.ErrorResultCount; i++)
        {
            yield return mapper.CreateErrorResult(i);
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
}
