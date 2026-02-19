using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;
using HotChocolate.Transport;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// HTTP-based implementation of <see cref="ISourceSchemaClient"/> that sends GraphQL operations
/// to a downstream service over HTTP. Supports single requests, Apollo-style request batching,
/// and variable batching depending on the configured <see cref="SourceSchemaHttpClientConfiguration.BatchingMode"/>.
/// </summary>
public sealed class SourceSchemaHttpClient : ISourceSchemaClient
{
    private static ReadOnlySpan<byte> VariableIndex => "variableIndex"u8;
    private static ReadOnlySpan<byte> RequestIndex => "requestIndex"u8;

    private readonly GraphQLHttpClient _client;
    private readonly SourceSchemaHttpClientConfiguration _configuration;
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
    }

    /// <inheritdoc />
    public async ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        var httpRequest = CreateHttpRequest(request);
        ConfigureCallbacks(httpRequest, context, request);

        var httpResponse = await _client.SendAsync(httpRequest, cancellationToken);
        return new Response(
            request.OperationType,
            httpRequest,
            httpResponse,
            request.Variables);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyDictionary<int, SourceSchemaClientResponse>> ExecuteBatchAsync(
        OperationPlanContext context,
        IReadOnlyList<SourceSchemaClientRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requests);

        if (requests.Count == 0)
        {
            return new Dictionary<int, SourceSchemaClientResponse>();
        }

        if (ContainsSubscriptionRequest(requests))
        {
            throw new InvalidOperationException("Subscription requests are not supported by batch execution.");
        }

        var buffers = CreateBuffers(requests);
        var (httpRequest, requestStartIndices, batchRequestCount) = CreateHttpBatchRequest(requests);
        ConfigureBatchCallbacks(httpRequest, context, requests);

        GraphQLHttpResponse? httpResponse = null;

        try
        {
            httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            ApplyTransportDetails(requests, buffers, httpRequest, httpResponse);

            await ReadBatchResultsAsync(
                    context,
                    requestStartIndices,
                    batchRequestCount,
                    buffers,
                    httpResponse,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            DisposeBuffers(buffers);
            throw;
        }
        finally
        {
            httpResponse?.Dispose();
        }

        return CreateBufferedResponses(requests, buffers);
    }

    /// <summary>
    /// Creates the appropriate <see cref="GraphQLHttpRequest"/> for the given request,
    /// choosing between a single operation, an Apollo operation batch, or a variable batch
    /// based on the number of variable sets and the configured batching mode.
    /// </summary>
    private GraphQLHttpRequest CreateHttpRequest(
        SourceSchemaClientRequest originalRequest)
    {
        var defaultAccept = originalRequest.OperationType is OperationType.Subscription
            ? _configuration.SubscriptionAcceptHeaderValues
            : _configuration.DefaultAcceptHeaderValues;
        var operationSourceText = originalRequest.OperationSourceText;

        switch (originalRequest.Variables.Length)
        {
            case 0:
                return new GraphQLHttpRequest(CreateSingleRequest(operationSourceText))
                {
                    Uri = _configuration.BaseAddress,
                    Accept = defaultAccept
                };

            case 1:
                var variableValues = originalRequest.Variables[0].Values;
                return new GraphQLHttpRequest(CreateSingleRequest(operationSourceText, variableValues))
                {
                    Uri = _configuration.BaseAddress,
                    Accept = defaultAccept,
                    EnableFileUploads = originalRequest.RequiresFileUpload
                };

            default:
                if (_configuration.BatchingMode == SourceSchemaHttpClientBatchingMode.ApolloRequestBatching)
                {
                    return new GraphQLHttpRequest(CreateOperationBatchRequest(operationSourceText, originalRequest))
                    {
                        Uri = _configuration.BaseAddress,
                        Accept = _configuration.BatchingAcceptHeaderValues,
                        EnableFileUploads = originalRequest.RequiresFileUpload
                    };
                }

                return new GraphQLHttpRequest(CreateVariableBatchRequest(operationSourceText, originalRequest))
                {
                    Uri = _configuration.BaseAddress,
                    Accept = _configuration.BatchingAcceptHeaderValues,
                    EnableFileUploads = originalRequest.RequiresFileUpload
                };
        }
    }

    private BatchRequestContext CreateHttpBatchRequest(
        IReadOnlyList<SourceSchemaClientRequest> originalRequests)
    {
        var requestStartIndices = new List<RequestStartEntry>(originalRequests.Count);
        var batchRequests = new List<IOperationRequest>();
        var enableFileUploads = false;

        for (var i = 0; i < originalRequests.Count; i++)
        {
            var sourceRequest = originalRequests[i];
            enableFileUploads |= sourceRequest.RequiresFileUpload;

            requestStartIndices.Add(new RequestStartEntry(batchRequests.Count, sourceRequest));

            var body = CreateRequestBody(sourceRequest);
            if (body is IOperationRequest operationRequest)
            {
                batchRequests.Add(operationRequest);
            }
            else if (body is OperationBatchRequest operationBatchRequest)
            {
                for (var requestIndex = 0; requestIndex < operationBatchRequest.Requests.Count; requestIndex++)
                {
                    batchRequests.Add(operationBatchRequest.Requests[requestIndex]);
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"The request body type '{body.GetType().Name}' cannot be included in an operation batch.");
            }
        }

        var httpRequest = new GraphQLHttpRequest(new OperationBatchRequest(batchRequests))
        {
            Uri = _configuration.BaseAddress,
            Accept = _configuration.BatchingAcceptHeaderValues,
            EnableFileUploads = enableFileUploads
        };

        return new BatchRequestContext(httpRequest, requestStartIndices, batchRequests.Count);
    }

    private IRequestBody CreateRequestBody(
        SourceSchemaClientRequest originalRequest)
    {
        var operationSourceText = originalRequest.OperationSourceText;

        switch (originalRequest.Variables.Length)
        {
            case 0:
                return CreateSingleRequest(operationSourceText);

            case 1:
                var variableValues = originalRequest.Variables[0].Values;
                return CreateSingleRequest(operationSourceText, variableValues);

            default:
                if (_configuration.BatchingMode == SourceSchemaHttpClientBatchingMode.ApolloRequestBatching)
                {
                    return CreateOperationBatchRequest(operationSourceText, originalRequest);
                }

                return CreateVariableBatchRequest(operationSourceText, originalRequest);
        }
    }

    private static OperationRequest CreateSingleRequest(
        string operationSourceText,
        ObjectValueNode? variables = null)
    {
        return new OperationRequest(
            operationSourceText,
            id: null,
            operationName: null,
            onError: null,
            variables: variables,
            extensions: null);
    }

    private static OperationBatchRequest CreateOperationBatchRequest(
        string operationSourceText,
        SourceSchemaClientRequest originalRequest)
    {
        var requests = new OperationRequest[originalRequest.Variables.Length];

        for (var i = 0; i < requests.Length; i++)
        {
            requests[i] = CreateSingleRequest(
                operationSourceText,
                originalRequest.Variables[i].Values);
        }

        return new OperationBatchRequest(requests);
    }

    private static VariableBatchRequest CreateVariableBatchRequest(
        string operationSourceText,
        SourceSchemaClientRequest originalRequest)
    {
        var variables = new ObjectValueNode[originalRequest.Variables.Length];

        for (var i = 0; i < originalRequest.Variables.Length; i++)
        {
            variables[i] = originalRequest.Variables[i].Values;
        }

        return new VariableBatchRequest(
            operationSourceText,
            id: null,
            operationName: null,
            onError: null,
            variables: variables,
            extensions: null);
    }

    private async ValueTask ReadBatchResultsAsync(
        OperationPlanContext context,
        IReadOnlyList<RequestStartEntry> requestStartIndices,
        int batchRequestCount,
        Dictionary<int, NodeResponseBuffer> buffers,
        GraphQLHttpResponse response,
        CancellationToken cancellationToken)
    {
        var fallbackRequestIndex = 0;
        var fallbackVariableIndices = new Dictionary<int, int>();

        await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cancellationToken))
        {
            int requestIndex;

            if (TryGetResultIndex(result, RequestIndex, out var requestIndexFromResult))
            {
                requestIndex = requestIndexFromResult;
            }
            else
            {
                requestIndex = fallbackRequestIndex++;
            }

            if ((uint)requestIndex >= (uint)batchRequestCount
                || !TryResolveRequest(requestStartIndices, requestIndex, out var requestStartEntry))
            {
                result.Dispose();
                continue;
            }

            var request = requestStartEntry.Request;

            if (!buffers.TryGetValue(request.Node.Id, out var buffer))
            {
                result.Dispose();
                continue;
            }

            var variableIndex = ResolveVariableIndex(
                request,
                requestIndex,
                requestStartEntry.StartIndex,
                fallbackVariableIndices,
                result);

            if (!TryGetResultPath(request, variableIndex, out var path, out var additionalPaths))
            {
                result.Dispose();
                continue;
            }

            AddResult(context, request.Node, buffer, path, additionalPaths, result);
        }
    }

    private static bool TryResolveRequest(
        IReadOnlyList<RequestStartEntry> requestStartIndices,
        int requestIndex,
        out RequestStartEntry requestStartEntry)
    {
        var left = 0;
        var right = requestStartIndices.Count - 1;
        requestStartEntry = default;

        while (left <= right)
        {
            var mid = left + ((right - left) >> 1);
            var current = requestStartIndices[mid];

            if (current.StartIndex <= requestIndex)
            {
                requestStartEntry = current;
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return right >= 0;
    }

    private int ResolveVariableIndex(
        SourceSchemaClientRequest request,
        int requestIndex,
        int requestStartIndex,
        Dictionary<int, int> fallbackVariableIndices,
        SourceResultDocument result)
    {
        var variableCount = request.Variables.Length;

        if (variableCount == 0)
        {
            return 0;
        }

        if (TryGetResultIndex(result, VariableIndex, out var variableIndex))
        {
            return (uint)variableIndex < (uint)variableCount
                ? variableIndex
                : variableCount;
        }

        if (variableCount == 1)
        {
            return 0;
        }

        if (_configuration.BatchingMode is SourceSchemaHttpClientBatchingMode.ApolloRequestBatching)
        {
            variableIndex = requestIndex - requestStartIndex;
            return (uint)variableIndex < (uint)variableCount
                ? variableIndex
                : variableCount;
        }

        if (!fallbackVariableIndices.TryGetValue(requestStartIndex, out var nextVariableIndex))
        {
            nextVariableIndex = 0;
        }

        if ((uint)nextVariableIndex >= (uint)variableCount)
        {
            return variableCount;
        }

        fallbackVariableIndices[requestStartIndex] = nextVariableIndex + 1;
        return nextVariableIndex;
    }

    private static bool TryGetResultPath(
        SourceSchemaClientRequest request,
        int variableIndex,
        out Path path,
        out ImmutableArray<Path> additionalPaths)
    {
        if (request.Variables.Length == 0)
        {
            path = Path.Root;
            additionalPaths = [];
            return true;
        }

        if ((uint)variableIndex >= (uint)request.Variables.Length)
        {
            path = Path.Root;
            additionalPaths = [];
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
    /// a single HTTP request.
    /// </summary>
    private void ConfigureCallbacks(
        GraphQLHttpRequest request,
        OperationPlanContext context,
        SourceSchemaClientRequest sourceRequest)
    {
        request.State = (context, sourceRequest.Node, _configuration);

        request.OnMessageCreated += static (_, requestMessage, state) =>
        {
            var (context, node, configuration) =
                ((OperationPlanContext, ExecutionNode, SourceSchemaHttpClientConfiguration))state!;
            configuration.OnBeforeSend?.Invoke(context, node, requestMessage);
        };

        request.OnMessageReceived += static (_, responseMessage, state) =>
        {
            var (context, node, configuration) =
                ((OperationPlanContext, ExecutionNode, SourceSchemaHttpClientConfiguration))state!;
            configuration.OnAfterReceive?.Invoke(context, node, responseMessage);
        };
    }

    /// <summary>
    /// Attaches <see cref="SourceSchemaHttpClientConfiguration.OnBeforeSend"/> and
    /// <see cref="SourceSchemaHttpClientConfiguration.OnAfterReceive"/> callbacks to
    /// the HTTP request, invoking them for each node in the batch.
    /// </summary>
    private void ConfigureBatchCallbacks(
        GraphQLHttpRequest request,
        OperationPlanContext context,
        IReadOnlyList<SourceSchemaClientRequest> requests)
    {
        request.State = (context, requests, _configuration);

        request.OnMessageCreated += static (_, requestMessage, state) =>
        {
            var (context, requests, configuration) =
                ((OperationPlanContext, IReadOnlyList<SourceSchemaClientRequest>, SourceSchemaHttpClientConfiguration))state!;

            for (var i = 0; i < requests.Count; i++)
            {
                configuration.OnBeforeSend?.Invoke(context, requests[i].Node, requestMessage);
            }
        };

        request.OnMessageReceived += static (_, responseMessage, state) =>
        {
            var (context, requests, configuration) =
                ((OperationPlanContext, IReadOnlyList<SourceSchemaClientRequest>, SourceSchemaHttpClientConfiguration))state!;

            for (var i = 0; i < requests.Count; i++)
            {
                configuration.OnAfterReceive?.Invoke(context, requests[i].Node, responseMessage);
            }
        };
    }

    private void AddResult(
        OperationPlanContext context,
        ExecutionNode node,
        NodeResponseBuffer buffer,
        Path path,
        ImmutableArray<Path> additionalPaths,
        SourceResultDocument document)
    {
        var sourceSchemaResult = new SourceSchemaResult(path, document);
        _configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);
        buffer.Results.Add(sourceSchemaResult);

        foreach (var additionalPath in additionalPaths)
        {
            var alias = sourceSchemaResult.WithPath(additionalPath);
            _configuration.OnSourceSchemaResult?.Invoke(context, node, alias);
            buffer.Results.Add(alias);
        }
    }

    private static Dictionary<int, NodeResponseBuffer> CreateBuffers(
        IReadOnlyList<SourceSchemaClientRequest> requests)
    {
        var buffers = new Dictionary<int, NodeResponseBuffer>(requests.Count);

        for (var i = 0; i < requests.Count; i++)
        {
            buffers[requests[i].Node.Id] = new NodeResponseBuffer();
        }

        return buffers;
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

    private static IReadOnlyDictionary<int, SourceSchemaClientResponse> CreateBufferedResponses(
        IReadOnlyList<SourceSchemaClientRequest> requests,
        Dictionary<int, NodeResponseBuffer> buffers)
    {
        var responses = new Dictionary<int, SourceSchemaClientResponse>(requests.Count);

        for (var i = 0; i < requests.Count; i++)
        {
            var nodeId = requests[i].Node.Id;

            if (!buffers.TryGetValue(nodeId, out var buffer))
            {
                continue;
            }

            responses[nodeId] = buffer.CreateResponse();
        }

        return responses;
    }

    private static void DisposeBuffers(Dictionary<int, NodeResponseBuffer> buffers)
    {
        foreach (var buffer in buffers.Values)
        {
            foreach (var result in buffer.Results)
            {
                result.Dispose();
            }

            buffer.Results.Clear();
        }
    }

    private static bool TryGetResultIndex(
        SourceResultDocument result,
        ReadOnlySpan<byte> propertyName,
        out int index)
    {
        if (result.Root.TryGetProperty(propertyName, out var value)
            && value.ValueKind is JsonValueKind.Number)
        {
            index = value.GetInt32();
            return true;
        }

        index = -1;
        return false;
    }

    private static void ApplyTransportDetails(
        IReadOnlyList<SourceSchemaClientRequest> requests,
        Dictionary<int, NodeResponseBuffer> buffers,
        GraphQLHttpRequest request,
        GraphQLHttpResponse response)
    {
        var uri = request.Uri ?? new Uri("http://unknown");
        var contentType = response.ContentHeaders.ContentType?.ToString() ?? "unknown";
        var isSuccessful = response.IsSuccessStatusCode;

        for (var i = 0; i < requests.Count; i++)
        {
            if (buffers.TryGetValue(requests[i].Node.Id, out var buffer))
            {
                buffer.SetTransport(uri, contentType, isSuccessful);
            }
        }
    }

    /// <summary>
    /// A live response backed by an in-flight HTTP response. Used for single (non-batched)
    /// requests where the response stream is read lazily on enumeration.
    /// </summary>
    private sealed class Response(
        OperationType operation,
        GraphQLHttpRequest request,
        GraphQLHttpResponse response,
        ImmutableArray<VariableValues> variables)
        : SourceSchemaClientResponse
    {
        public override async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var (context, node, configuration) =
                ((OperationPlanContext, ExecutionNode, SourceSchemaHttpClientConfiguration))request.State!;

            if (operation == OperationType.Subscription)
            {
                await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cancellationToken))
                {
                    var sourceSchemaResult = new SourceSchemaResult(Path.Root, result);

                    configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                    yield return sourceSchemaResult;
                }
            }
            else
            {
                switch (variables.Length)
                {
                    case 0:
                    {
                        var result = await response.ReadAsResultAsync(cancellationToken);
                        var sourceSchemaResult = new SourceSchemaResult(Path.Root, result);

                        configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                        yield return sourceSchemaResult;
                        break;
                    }

                    case 1:
                    {
                        var result = await response.ReadAsResultAsync(cancellationToken);
                        var variable = variables[0];
                        var sourceSchemaResult = new SourceSchemaResult(variable.Path, result);

                        configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                        yield return sourceSchemaResult;

                        foreach (var additionalPath in variable.AdditionalPaths)
                        {
                            var alias = sourceSchemaResult.WithPath(additionalPath);
                            configuration.OnSourceSchemaResult?.Invoke(context, node, alias);
                            yield return alias;
                        }

                        break;
                    }

                    default:
                    {
                        SourceSchemaResult? errorResult = null;

                        if (configuration.BatchingMode == SourceSchemaHttpClientBatchingMode.ApolloRequestBatching)
                        {
                            var requestIndex = 0;
                            await foreach (var result in response.ReadAsResultStreamAsync()
                                .WithCancellation(cancellationToken))
                            {
                                var variable = variables[requestIndex];
                                var sourceSchemaResult = new SourceSchemaResult(variable.Path, result);

                                configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                                yield return sourceSchemaResult;

                                foreach (var additionalPath in variable.AdditionalPaths)
                                {
                                    var alias = sourceSchemaResult.WithPath(additionalPath);
                                    configuration.OnSourceSchemaResult?.Invoke(context, node, alias);
                                    yield return alias;
                                }

                                requestIndex++;
                            }
                        }
                        else
                        {
                            await foreach (var result in response.ReadAsResultStreamAsync()
                                .WithCancellation(cancellationToken))
                            {
                                if (!result.Root.TryGetProperty(VariableIndex, out var variableIndex)
                                    || variableIndex.ValueKind is not JsonValueKind.Number)
                                {
                                    errorResult = new SourceSchemaResult(variables[0].Path, result);
                                    configuration.OnSourceSchemaResult?.Invoke(context, node, errorResult);
                                    break;
                                }

                                var index = variableIndex.GetInt32();
                                var variable = variables[index];
                                var sourceSchemaResult = new SourceSchemaResult(variable.Path, result);

                                configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                                yield return sourceSchemaResult;

                                foreach (var additionalPath in variable.AdditionalPaths)
                                {
                                    var alias = sourceSchemaResult.WithPath(additionalPath);
                                    configuration.OnSourceSchemaResult?.Invoke(context, node, alias);
                                    yield return alias;
                                }
                            }
                        }

                        if (errorResult is not null)
                        {
                            yield return errorResult;

                            foreach (var additionalPath in variables[0].AdditionalPaths)
                            {
                                var alias = errorResult.WithPath(additionalPath);
                                configuration.OnSourceSchemaResult?.Invoke(context, node, alias);
                                yield return alias;
                            }

                            for (var i = 1; i < variables.Length; i++)
                            {
                                var variable = variables[i];
                                var sourceSchemaResult = new SourceSchemaResult(
                                    variable.Path,
                                    SourceResultDocument.CreateEmptyObject());
                                yield return sourceSchemaResult;

                                foreach (var additionalPath in variable.AdditionalPaths)
                                {
                                    var alias = sourceSchemaResult.WithPath(additionalPath);
                                    configuration.OnSourceSchemaResult?.Invoke(context, node, alias);
                                    yield return alias;
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        public override Uri Uri => request.Uri ?? new Uri("http://unknown");

        public override string ContentType => response.ContentHeaders.ContentType?.ToString() ?? "unknown";

        public override bool IsSuccessful => response.IsSuccessStatusCode;

        public override void Dispose() => response.Dispose();
    }

    /// <summary>
    /// A response backed by an already-materialized list of results. Used for batched
    /// requests where results are collected per-node before being returned.
    /// </summary>
    private sealed class BufferedResponse(
        Uri uri,
        string contentType,
        bool isSuccessful,
        IReadOnlyList<SourceSchemaResult> results)
        : SourceSchemaClientResponse
    {
        private int _nextResultIndex;
        private bool _disposed;

        public override Uri Uri { get; } = uri;

        public override string ContentType { get; } = contentType;

        public override bool IsSuccessful { get; } = isSuccessful;

        public override async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var index = _nextResultIndex;

                if ((uint)index >= (uint)results.Count)
                {
                    yield break;
                }

                _nextResultIndex = index + 1;
                yield return results[index];
            }
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Results that were already yielded are owned by the consumer.
            // We only dispose unread items if the response is disposed early.
            for (var i = _nextResultIndex; i < results.Count; i++)
            {
                results[i].Dispose();
            }
        }
    }

    private readonly record struct RequestStartEntry(
        int StartIndex,
        SourceSchemaClientRequest Request);

    private readonly ref struct BatchRequestContext(
        GraphQLHttpRequest httpRequest,
        IReadOnlyList<RequestStartEntry> requestStartIndices,
        int batchRequestCount)
    {
        public GraphQLHttpRequest HttpRequest { get; } = httpRequest;

        public IReadOnlyList<RequestStartEntry> RequestStartIndices { get; } = requestStartIndices;

        public int BatchRequestCount { get; } = batchRequestCount;

        public void Deconstruct(
            out GraphQLHttpRequest httpRequest,
            out IReadOnlyList<RequestStartEntry> requestStartIndices,
            out int batchRequestCount)
        {
            httpRequest = HttpRequest;
            requestStartIndices = RequestStartIndices;
            batchRequestCount = BatchRequestCount;
        }
    }

    /// <summary>
    /// Accumulates results and transport metadata for a single execution node
    /// during a batched request, then produces a <see cref="BufferedResponse"/>.
    /// </summary>
    private sealed class NodeResponseBuffer
    {
        private Uri _uri = new("http://unknown");
        private string _contentType = "unknown";
        private bool _isSuccessful = true;

        public List<SourceSchemaResult> Results { get; } = [];

        public void SetTransport(Uri uri, string contentType, bool isSuccessful)
        {
            _uri = uri;
            _contentType = contentType;
            _isSuccessful = isSuccessful;
        }

        public SourceSchemaClientResponse CreateResponse()
            => new BufferedResponse(_uri, _contentType, _isSuccessful, Results);
    }
}
