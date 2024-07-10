using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using StrawberryShake.Internal;
using static System.Text.Json.JsonDocument;
using static StrawberryShake.Properties.Resources;

namespace StrawberryShake.Transport.InMemory;

public class InMemoryConnection : IInMemoryConnection
{
    private readonly Func<CancellationToken, ValueTask<IInMemoryClient>> _createClientAsync;

    public InMemoryConnection(
        Func<CancellationToken, ValueTask<IInMemoryClient>> createClientAsync)
    {
        _createClientAsync = createClientAsync ??
            throw new ArgumentNullException(nameof(createClientAsync));
    }

    public IAsyncEnumerable<Response<JsonDocument>> ExecuteAsync(
        OperationRequest request)
        => new ResponseStream(_createClientAsync, request);

    private sealed class ResponseStream : IAsyncEnumerable<Response<JsonDocument>>
    {
        private readonly Func<CancellationToken, ValueTask<IInMemoryClient>> _createClientAsync;
        private readonly OperationRequest _request;

        public ResponseStream(
            Func<CancellationToken, ValueTask<IInMemoryClient>> createClientAsync,
            OperationRequest request)
        {
            _createClientAsync = createClientAsync;
            _request = request;
        }

        public async IAsyncEnumerator<Response<JsonDocument>> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            var client = await _createClientAsync(cancellationToken);

            Exception? exception = null;
            IExecutionResult? result = null;

            try
            {
                result = await client.ExecuteAsync(_request, cancellationToken);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception is not null || result is null)
            {
                exception ??= new InvalidOperationException("No result found!");

                yield return new Response<JsonDocument>(
                    ResponseHelper.CreateBodyFromException(exception),
                    exception);
                yield break;
            }

            await foreach (var response in ProcessResultAsync(result, cancellationToken)
                .ConfigureAwait(false))
            {
                yield return response;
            }
        }

        private async IAsyncEnumerable<Response<JsonDocument>> ProcessResultAsync(
            IExecutionResult executionResult,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var writer = new ArrayWriter();

            switch (executionResult)
            {
                case HotChocolate.Execution.IOperationResult queryResult:
                {
                    queryResult.WriteTo(writer);
                    yield return new Response<JsonDocument>(Parse(writer.GetWrittenMemory()), null);
                    break;
                }

                case HotChocolate.Execution.ResponseStream streamResult:
                {
                    await foreach (var result in streamResult.ReadResultsAsync().WithCancellation(cancellationToken))
                    {
                        result.WriteTo(writer);
                        var document = Parse(writer.GetWrittenMemory());
                        writer.Reset();

                        yield return new Response<JsonDocument>(document, null);
                    }
                }

                    break;

                default:
                {
                    var ex = new GraphQLClientException(InMemoryConnection_InvalidResponseFormat);
                    yield return new Response<JsonDocument>(
                        ResponseHelper.CreateBodyFromException(ex),
                        ex);
                    yield break;
                }
            }
        }
    }
}
