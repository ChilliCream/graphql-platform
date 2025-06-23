using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Caching.Memory;
using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion.Execution.Clients;

public class SourceSchemaHttpClient : ISourceSchemaClient
{
    private readonly GraphQLHttpClient _client;
    private readonly Cache<string> _operationStringCache;

    public SourceSchemaHttpClient(
        GraphQLHttpClient client,
        Cache<string> operationStringCache)
    {
        _client = client
            ?? throw new ArgumentNullException(nameof(client));
        _operationStringCache = operationStringCache
            ?? throw new ArgumentNullException(nameof(operationStringCache));
    }

    public async ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        var httpRequest = CreateHttpRequest(request);
        var httpResponse = await _client.SendAsync(httpRequest, cancellationToken);
        return new Response(httpResponse, request.Variables);
    }

    private GraphQLHttpRequest CreateHttpRequest(
        SourceSchemaClientRequest originalRequest)
    {
        var operationSourceText =
            _operationStringCache.GetOrCreate(
                originalRequest.OperationId,
                (_, o) => o.ToString(),
                originalRequest.Operation);

        switch (originalRequest.Variables.Length)
        {
            case 0:
                return new GraphQLHttpRequest(
                    CreateSingleRequest(operationSourceText));

            case 1:
                return new GraphQLHttpRequest(
                    CreateSingleRequest(
                        operationSourceText,
                        originalRequest.Variables[0].Values));

            default:
                return new GraphQLHttpRequest(
                    CreateBatchRequest(
                        operationSourceText,
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
            variables: variables,
            extensions: null);
    }

    private sealed class Response(
        GraphQLHttpResponse response,
        ImmutableArray<VariableValues> variables)
        : SourceSchemaClientResponse
    {
        public override async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                    await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cancellationToken))
                    {
                        var index = result.VariableIndex!.Value;
                        var (path, _) = variables[index];
                        yield return new SourceSchemaResult(
                            path,
                            result,
                            result.Data,
                            result.Errors,
                            result.Extensions);
                    }

                    break;
                }
            }
        }

        public override bool IsSuccessful => response.IsSuccessStatusCode;

        public override void Dispose() => response.Dispose();
    }
}
