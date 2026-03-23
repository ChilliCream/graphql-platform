using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    public async IAsyncEnumerable<BatchStreamResult> ExecuteBatchStreamAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (requests.Length == 0)
        {
            yield break;
        }

        if (ContainsSubscriptionRequest(requests))
        {
            throw new InvalidOperationException(
                FusionExecutionResources.SourceSchemaHttpClient_SubscriptionBatchNotSupported);
        }

        var httpRequest = CreateHttpBatchRequest(requests);
        ConfigureBatchCallbacks(httpRequest, context, requests);

        var httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

        try
        {
            await foreach (var result in httpResponse.ReadAsResultStreamAsync().WithCancellation(cancellationToken))
            {
                var requestIndex = ResolveRequestIndex(requests, result);

                // When the subgraph returns a blanket error without a
                // requestIndex, the error applies to every request in the
                // batch. We yield the same result for each request so
                // downstream error handling sees the error on every path.
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

                        _configuration.OnSourceSchemaResult?.Invoke(context, req.Node, ssr);
                        yield return new BatchStreamResult(i, ssr);
                    }

                    continue;
                }

                var request = requests[requestIndex];
                var variableIndex = ResolveVariableIndex(request, result);

                // When the response lacks a variableIndex, the error
                // applies to every variable set in this request.
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

                        _configuration.OnSourceSchemaResult?.Invoke(context, request.Node, vssr);
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

                var onSourceSchemaResult = _configuration.OnSourceSchemaResult;
                onSourceSchemaResult?.Invoke(context, request.Node, sourceSchemaResult);

                if (onSourceSchemaResult is not null && !additionalPaths.IsDefaultOrEmpty)
                {
                    foreach (var additionalPath in additionalPaths)
                    {
                        onSourceSchemaResult(context, request.Node, sourceSchemaResult.WithPath(additionalPath));
                    }
                }

                yield return new BatchStreamResult(requestIndex, sourceSchemaResult);
            }
        }
        finally
        {
            httpResponse.Dispose();
        }
    }

    /// <summary>
    /// Creates the appropriate <see cref="GraphQLHttpRequest"/> for the given request,
    /// choosing between a single operation, an Apollo operation batch, or a variable batch
    /// based on the number of variable sets and the configured batching mode.
    /// </summary>
    private GraphQLHttpRequest CreateHttpRequest(
        SourceSchemaClientRequest originalRequest)
    {
        var defaultAcceptHeader = originalRequest.OperationType is OperationType.Subscription
            ? _configuration.SubscriptionAcceptHeaderValue
            : _configuration.DefaultAcceptHeaderValue;
        var operationSourceText = originalRequest.OperationSourceText;

        switch (originalRequest.Variables.Length)
        {
            case 0:
                return new GraphQLHttpRequest(CreateSingleRequest(operationSourceText))
                {
                    Uri = _configuration.BaseAddress,
                    AcceptHeaderValue = defaultAcceptHeader
                };

            case 1:
                var variableValues = originalRequest.Variables[0].Values;
                return new GraphQLHttpRequest(CreateSingleRequest(operationSourceText, variableValues))
                {
                    Uri = _configuration.BaseAddress,
                    AcceptHeaderValue = defaultAcceptHeader,
                    EnableFileUploads = originalRequest.RequiresFileUpload
                };

            default:
                if (_configuration.BatchingMode == SourceSchemaHttpClientBatchingMode.ApolloRequestBatching)
                {
                    return new GraphQLHttpRequest(CreateOperationBatchRequest(operationSourceText, originalRequest))
                    {
                        Uri = _configuration.BaseAddress,
                        AcceptHeaderValue = _configuration.BatchingAcceptHeaderValue,
                        EnableFileUploads = originalRequest.RequiresFileUpload
                    };
                }

                return new GraphQLHttpRequest(CreateVariableBatchRequest(operationSourceText, originalRequest))
                {
                    Uri = _configuration.BaseAddress,
                    AcceptHeaderValue = _configuration.BatchingAcceptHeaderValue,
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
            AcceptHeaderValue = _configuration.BatchingAcceptHeaderValue,
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

    private static bool TryGetResultPath(
        SourceSchemaClientRequest request,
        int variableIndex,
        out CompactPath path,
        out ImmutableArray<CompactPath> additionalPaths)
    {
        if (request.Variables.Length == 0)
        {
            path = CompactPath.Root;
            additionalPaths = [];
            return true;
        }

        if ((uint)variableIndex >= (uint)request.Variables.Length)
        {
            path = CompactPath.Root;
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

        public override string ContentType => response.RawContentType ?? "unknown";

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
                    var sourceSchemaResult = new SourceSchemaResult(CompactPath.Root, result);

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
                        var sourceSchemaResult = new SourceSchemaResult(CompactPath.Root, result);

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

        public override void Dispose() => response.Dispose();
    }
}
