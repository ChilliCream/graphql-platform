using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;
using HotChocolate.Transport;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaHttpClient : ISourceSchemaClient
{
    private static ReadOnlySpan<byte> VariableIndex => "variableIndex"u8;

    private readonly GraphQLHttpClient _client;
    private readonly SourceSchemaHttpClientConfiguration _configuration;
    private bool _disposed;

    public SourceSchemaHttpClient(
        GraphQLHttpClient client,
        SourceSchemaHttpClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configuration);

        _client = client;
        _configuration = configuration;
    }

    public async ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        OperationPlanContext context,
        ExecutionNode node,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        var httpRequest = CreateHttpRequest(request);
        httpRequest.State = (context, node, _configuration);

        httpRequest.OnMessageCreated += static (_, requestMessage, state) =>
        {
            var (context, node, configuration) = ((OperationPlanContext, ExecutionNode, SourceSchemaHttpClientConfiguration))state!;
            configuration.OnBeforeSend?.Invoke(context, node, requestMessage);
        };

        httpRequest.OnMessageReceived += static (_, responseMessage, state) =>
        {
            var (context, node, configuration) = ((OperationPlanContext, ExecutionNode, SourceSchemaHttpClientConfiguration))state!;
            configuration.OnAfterReceive?.Invoke(context, node, responseMessage);
        };

        var httpResponse = await _client.SendAsync(httpRequest, cancellationToken);
        return new Response(
            request.OperationType,
            httpRequest,
            httpResponse,
            request.Variables);
    }

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
                        var sourceSchemaResult = new SourceSchemaResult(variables[0].Path, result);

                        configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                        yield return sourceSchemaResult;
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
                                var (path, _) = variables[requestIndex];

                                var sourceSchemaResult = new SourceSchemaResult(path, result);

                                configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                                yield return sourceSchemaResult;

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
                                var (path, _) = variables[index];
                                var sourceSchemaResult = new SourceSchemaResult(path, result);

                                configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                                yield return sourceSchemaResult;
                            }
                        }

                        if (errorResult is not null)
                        {
                            yield return errorResult;

                            for (var i = 1; i < variables.Length; i++)
                            {
                                var (path, _) = variables[i];
                                yield return new SourceSchemaResult(path, SourceResultDocument.CreateEmptyObject());
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
}
