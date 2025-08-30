using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
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
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        var httpRequest = CreateHttpRequest(request);
        httpRequest.State = (context, _configuration);

        httpRequest.OnMessageCreated += static (_, requestMessage, state) =>
        {
            var (context, configuration) = ((OperationPlanContext, SourceSchemaHttpClientConfiguration))state!;
            configuration.OnBeforeSend(context, requestMessage);
        };

        httpRequest.OnMessageReceived += static (_, responseMessage, state) =>
        {
            var (context, configuration) = ((OperationPlanContext, SourceSchemaHttpClientConfiguration))state!;
            configuration.OnAfterReceive(context, responseMessage);
        };

        var httpResponse = await _client.SendAsync(httpRequest, cancellationToken);
        return new Response(request.OperationType, httpResponse, request.Variables);
    }

    private GraphQLHttpRequest CreateHttpRequest(
        SourceSchemaClientRequest originalRequest)
    {
        switch (originalRequest.Variables.Length)
        {
            case 0:
                return new GraphQLHttpRequest(
                    CreateSingleRequest(
                        originalRequest.OperationSourceText));

            case 1:
                return new GraphQLHttpRequest(
                    CreateSingleRequest(
                        originalRequest.OperationSourceText,
                        originalRequest.Variables[0].Values));

            default:
                return new GraphQLHttpRequest(
                    CreateBatchRequest(
                        originalRequest.OperationSourceText,
                        originalRequest));
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
        GraphQLHttpResponse response,
        ImmutableArray<VariableValues> variables)
        : SourceSchemaClientResponse
    {
        public override async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (operation == OperationType.Subscription)
            {
                await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cancellationToken))
                {
                    yield return new SourceSchemaResult(
                        Path.Root,
                        result,
                        result.Data,
                        result.Errors,
                        result.Extensions);
                }
            }
            else
            {
                switch (variables.Length)
                {
                    case 0:
                    {
                        var result = await response.ReadAsResultAsync(cancellationToken);
                        yield return new SourceSchemaResult(
                            Path.Root,
                            result,
                            result.Data,
                            result.Errors,
                            result.Extensions);
                        break;
                    }

                    case 1:
                    {
                        var result = await response.ReadAsResultAsync(cancellationToken);
                        yield return new SourceSchemaResult(
                            variables[0].Path,
                            result,
                            result.Data,
                            result.Errors,
                            result.Extensions);
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
                                break;
                            }

                            var index = result.VariableIndex!.Value;
                            var (path, _) = variables[index];
                            yield return new SourceSchemaResult(
                                path,
                                result,
                                result.Data,
                                result.Errors,
                                result.Extensions);
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

        public override bool IsSuccessful => response.IsSuccessStatusCode;

        public override void Dispose() => response.Dispose();
    }
}
