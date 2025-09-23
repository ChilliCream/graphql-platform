using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaHttpClient : ISourceSchemaClient
{
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
            ? AcceptContentTypes.Subscription
            : AcceptContentTypes.Default;
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
                    Accept = defaultAccept
                };

            default:
                return new GraphQLHttpRequest(CreateBatchRequest(operationSourceText, originalRequest))
                {
                    Uri = _configuration.BaseAddress,
                    Accept = AcceptContentTypes.VariableBatching
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

    private static VariableBatchRequest CreateBatchRequest(
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
                    var sourceSchemaResult = new SourceSchemaResult(
                        Path.Root,
                        result,
                        result.Data,
                        result.Errors,
                        result.Extensions);

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
                        var sourceSchemaResult = new SourceSchemaResult(
                            Path.Root,
                            result,
                            result.Data,
                            result.Errors,
                            result.Extensions);

                        configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                        yield return sourceSchemaResult;
                        break;
                    }

                    case 1:
                    {
                        var result = await response.ReadAsResultAsync(cancellationToken);
                        var sourceSchemaResult = new SourceSchemaResult(
                            variables[0].Path,
                            result,
                            result.Data,
                            result.Errors,
                            result.Extensions);

                        configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                        yield return sourceSchemaResult;
                        break;
                    }

                    default:
                    {
                        SourceSchemaResult? errorResult = null;

                        await foreach (var result in response.ReadAsResultStreamAsync()
                            .WithCancellation(cancellationToken))
                        {
                            if (result.VariableIndex is null)
                            {
                                errorResult = new SourceSchemaResult(
                                    variables[0].Path,
                                    result,
                                    result.Data,
                                    result.Errors,
                                    result.Extensions);

                                configuration.OnSourceSchemaResult?.Invoke(context, node, errorResult);
                                break;
                            }

                            var index = result.VariableIndex!.Value;
                            var (path, _) = variables[index];
                            var sourceSchemaResult = new SourceSchemaResult(
                                path,
                                result,
                                result.Data,
                                result.Errors,
                                result.Extensions);

                            configuration.OnSourceSchemaResult?.Invoke(context, node, sourceSchemaResult);

                            yield return sourceSchemaResult;
                        }

                        if (errorResult is not null)
                        {
                            yield return errorResult;

                            for (var i = 1; i < variables.Length; i++)
                            {
                                var (path, _) = variables[i];
                                yield return new SourceSchemaResult(
                                    path,
                                    Disposable.Empty,
                                    default,
                                    default,
                                    default);
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

    private static class AcceptContentTypes
    {
        public static readonly ImmutableArray<MediaTypeWithQualityHeaderValue> Default =
        [
            new("application/graphql-response+json") { CharSet = "utf-8" },
            new("application/json") { CharSet = "utf-8" },
            new("application/jsonl") { CharSet = "utf-8" },
            new("text/event-stream") { CharSet = "utf-8" }
        ];

        public static ImmutableArray<MediaTypeWithQualityHeaderValue> VariableBatching { get; } =
        [
            new("application/jsonl") { CharSet = "utf-8" },
            new("text/event-stream") { CharSet = "utf-8" }
        ];

        public static ImmutableArray<MediaTypeWithQualityHeaderValue> Subscription { get; } =
        [
            new("application/jsonl") { CharSet = "utf-8" },
            new("text/event-stream") { CharSet = "utf-8" }
        ];
    }
}
