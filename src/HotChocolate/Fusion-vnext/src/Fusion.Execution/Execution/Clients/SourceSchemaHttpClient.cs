using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Properties;
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

        var capabilities = SourceSchemaClientCapabilities.FileUpload;

        if (configuration.BatchingMode.HasFlag(SourceSchemaHttpClientBatchingMode.VariableBatching))
        {
            capabilities |= SourceSchemaClientCapabilities.VariableBatching;
        }

        if (configuration.BatchingMode.HasFlag(SourceSchemaHttpClientBatchingMode.RequestBatching))
        {
            capabilities |= SourceSchemaClientCapabilities.RequestBatching;
        }

        if (configuration.BatchingMode.HasFlag(SourceSchemaHttpClientBatchingMode.ApolloRequestBatching))
        {
            capabilities |= SourceSchemaClientCapabilities.ApolloRequestBatching;
        }

        Capabilities = capabilities;
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
        ArgumentNullException.ThrowIfNull(request);

        Debug.WriteLine(request.SchemaName);

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
    public async ValueTask<ImmutableArray<SourceSchemaClientResponse>> ExecuteBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (requests.Length == 0)
        {
            return [];
        }

        Debug.WriteLine(requests[0].SchemaName);

        if (ContainsSubscriptionRequest(requests))
        {
            throw new InvalidOperationException(
                FusionExecutionResources.SourceSchemaHttpClient_SubscriptionBatchNotSupported);
        }

        var httpRequest = CreateHttpBatchRequest(requests);
        ConfigureBatchCallbacks(httpRequest, context, requests);

        var httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

        var uri = httpRequest.Uri ?? new Uri("http://unknown");
        var contentType = httpResponse.ContentHeaders.ContentType?.ToString() ?? "unknown";
        var isSuccessful = httpResponse.IsSuccessStatusCode;

        var nodeResponses = new NodeResponse[requests.Length];
        var builder = ImmutableArray.CreateBuilder<SourceSchemaClientResponse>(requests.Length);

        for (var i = 0; i < requests.Length; i++)
        {
            var nodeResponse = new NodeResponse(uri, contentType, isSuccessful);
            nodeResponses[i] = nodeResponse;
            builder.Add(nodeResponse);
        }

        _ = ReadBatchStreamInBackgroundAsync(
                context,
                requests,
                nodeResponses,
                httpResponse,
                cancellationToken);

        return builder.MoveToImmutable();
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

    private GraphQLHttpRequest CreateHttpBatchRequest(
        IReadOnlyList<SourceSchemaClientRequest> originalRequests)
    {
        var batchRequests = new List<IOperationRequest>(originalRequests.Count);
        var enableFileUploads = false;

        for (var i = 0; i < originalRequests.Count; i++)
        {
            var sourceRequest = originalRequests[i];
            enableFileUploads |= sourceRequest.RequiresFileUpload;

            var body = CreateRequestBody(sourceRequest);
            if (body is IOperationRequest operationRequest)
            {
                batchRequests.Add(operationRequest);
            }
            else
            {
                throw new InvalidOperationException(
                    $"The request body type '{body.GetType().Name}' cannot be included in an operation batch.");
            }
        }

        return new GraphQLHttpRequest(new OperationBatchRequest(batchRequests))
        {
            Uri = _configuration.BaseAddress,
            Accept = _configuration.BatchingAcceptHeaderValues,
            EnableFileUploads = enableFileUploads
        };
    }

    private static IRequestBody CreateRequestBody(
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

    private async Task ReadBatchStreamInBackgroundAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        NodeResponse[] nodeResponses,
        GraphQLHttpResponse httpResponse,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var result in httpResponse.ReadAsResultStreamAsync()
                .WithCancellation(cancellationToken))
            {
                var requestIndex = result.Root.GetProperty(RequestIndex).GetInt32();

                if ((uint)requestIndex >= (uint)requests.Length)
                {
                    result.Dispose();
                    throw new InvalidOperationException(
                        string.Format(
                            FusionExecutionResources.SourceSchemaHttpClient_InvalidRequestIndex,
                            requestIndex));
                }

                var request = requests[requestIndex];
                var nodeResponse = nodeResponses[requestIndex];

                var variableIndex = ResolveVariableIndex(request, result);

                if (!TryGetResultPath(request, variableIndex, out var path, out var additionalPaths))
                {
                    result.Dispose();
                    throw new InvalidOperationException(
                        string.Format(
                            FusionExecutionResources.SourceSchemaHttpClient_InvalidVariableIndex,
                            variableIndex,
                            request.Node.Id));
                }

                WriteResultToChannel(context, request.Node, nodeResponse, path, additionalPaths, result);
            }

            // Stream completed successfully. Complete all channels, failing any
            // that never received results (fail-loud).
            for (var i = 0; i < nodeResponses.Length; i++)
            {
                var nodeResponse = nodeResponses[i];

                if (!nodeResponse.HasReceivedResults)
                {
                    nodeResponse.Complete(
                        new InvalidOperationException(
                            string.Format(
                                FusionExecutionResources.SourceSchemaHttpClient_NoResultForNode,
                                requests[i].Node.Id)));
                }
                else
                {
                    nodeResponse.Complete();
                }
            }
        }
        catch (Exception ex)
        {
            for (var i = 0; i < nodeResponses.Length; i++)
            {
                nodeResponses[i].Complete(ex);
            }
        }
        finally
        {
            httpResponse.Dispose();
        }
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

        var variableIndex = result.Root.GetProperty(VariableIndex).GetInt32();

        if ((uint)variableIndex < (uint)variableCount)
        {
            return variableIndex;
        }

        throw new InvalidOperationException(
            $"The batch response contains an out-of-range variableIndex '{variableIndex}'.");
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

    private void WriteResultToChannel(
        OperationPlanContext context,
        ExecutionNode node,
        NodeResponse nodeResponse,
        Path path,
        ImmutableArray<Path> additionalPaths,
        SourceResultDocument document)
    {
        var sourceSchemaResult = additionalPaths.IsDefaultOrEmpty
            ? new SourceSchemaResult(path, document)
            : new SourceSchemaResult(path, document, additionalPaths: additionalPaths);
        var onSourceSchemaResult = _configuration.OnSourceSchemaResult;

        onSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

        if (!nodeResponse.TryWrite(sourceSchemaResult))
        {
            sourceSchemaResult.Dispose();
            return;
        }

        nodeResponse.HasReceivedResults = true;

        if (onSourceSchemaResult is null || additionalPaths.IsDefaultOrEmpty)
        {
            return;
        }

        // Preserve callback behavior for all logical result paths without enqueueing aliases.
        foreach (var additionalPath in additionalPaths)
        {
            onSourceSchemaResult(context, node, sourceSchemaResult.WithPath(additionalPath));
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
        OperationType operation,
        GraphQLHttpRequest request,
        GraphQLHttpResponse response,
        ImmutableArray<VariableValues> variables)
        : SourceSchemaClientResponse
    {
        public override Uri Uri => request.Uri ?? new Uri("http://unknown");

        public override string ContentType => response.ContentHeaders.ContentType?.ToString() ?? "unknown";

        public override bool IsSuccessful => response.IsSuccessStatusCode;

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

                        var additionalPaths = variable.AdditionalPaths;

                        for (var i = 0; i < additionalPaths.Length; i++)
                        {
                            var additionalPath = additionalPaths[i];
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
                                if ((uint)requestIndex >= (uint)variables.Length)
                                {
                                    errorResult = new SourceSchemaResult(variables[0].Path, result);
                                    configuration.OnSourceSchemaResult?.Invoke(context, node, errorResult);
                                    break;
                                }

                                var variable = variables[requestIndex];
                                var sourceSchemaResult = new SourceSchemaResult(variable.Path, result);

                                configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                                yield return sourceSchemaResult;

                                var additionalPaths = variable.AdditionalPaths;

                                for (var i = 0; i < additionalPaths.Length; i++)
                                {
                                    var additionalPath = additionalPaths[i];
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

                                if ((uint)index >= (uint)variables.Length)
                                {
                                    errorResult = new SourceSchemaResult(variables[0].Path, result);
                                    configuration.OnSourceSchemaResult?.Invoke(context, node, errorResult);
                                    break;
                                }

                                var variable = variables[index];
                                var sourceSchemaResult = new SourceSchemaResult(variable.Path, result);

                                configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                                yield return sourceSchemaResult;

                                var additionalPaths = variable.AdditionalPaths;

                                for (var i = 0; i < additionalPaths.Length; i++)
                                {
                                    var additionalPath = additionalPaths[i];
                                    var alias = sourceSchemaResult.WithPath(additionalPath);
                                    configuration.OnSourceSchemaResult?.Invoke(context, node, alias);
                                    yield return alias;
                                }
                            }
                        }

                        if (errorResult is not null)
                        {
                            yield return errorResult;

                            var errorAdditionalPaths = variables[0].AdditionalPaths;

                            for (var i = 0; i < errorAdditionalPaths.Length; i++)
                            {
                                var additionalPath = errorAdditionalPaths[i];
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

                                var additionalPaths = variable.AdditionalPaths;

                                for (var j = 0; j < additionalPaths.Length; j++)
                                {
                                    var additionalPath = additionalPaths[j];
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

        public override void Dispose() => response.Dispose();
    }

    /// <summary>
    /// A streaming response for a single execution node within a batched HTTP request.
    /// Results are pushed into a <see cref="ConcurrentQueue{T}"/> by the background stream
    /// reader and signalled via a lightweight <see cref="AsyncAutoResetEvent"/>.
    /// The execution node reads lazily via <see cref="ReadAsResultStreamAsync"/>.
    /// </summary>
    private sealed class NodeResponse : SourceSchemaClientResponse
    {
        private readonly ConcurrentQueue<SourceSchemaResult> _results = new();
        private readonly AsyncAutoResetEvent _signal = new();
        private volatile bool _completed;
        private Exception? _error;
        private bool _disposed;

        public NodeResponse(Uri uri, string contentType, bool isSuccessful)
        {
            Uri = uri;
            ContentType = contentType;
            IsSuccessful = isSuccessful;
        }

        public override Uri Uri { get; }

        public override string ContentType { get; }

        public override bool IsSuccessful { get; }

        /// <summary>
        /// Gets whether at least one result has been written to this response.
        /// Used to detect nodes that received no results from the batch stream.
        /// </summary>
        internal bool HasReceivedResults { get; set; }

        internal bool TryWrite(SourceSchemaResult result)
        {
            if (_disposed)
            {
                return false;
            }

            _results.Enqueue(result);
            _signal.Set();
            return true;
        }

        internal void Complete(Exception? error = null)
        {
            _error = error;
            _completed = true;
            _signal.Set();
        }

        public override async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                while (_results.TryDequeue(out var result))
                {
                    yield return result;
                }

                if (_completed)
                {
                    // Final drain — writer may have enqueued between our last
                    // TryDequeue and the completion flag becoming visible.
                    while (_results.TryDequeue(out var result))
                    {
                        yield return result;
                    }

                    if (_error is not null)
                    {
                        ExceptionDispatchInfo.Throw(_error);
                    }

                    yield break;
                }

                await _signal;
            }
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            Complete();

            while (_results.TryDequeue(out var result))
            {
                result.Dispose();
            }
        }
    }
}
