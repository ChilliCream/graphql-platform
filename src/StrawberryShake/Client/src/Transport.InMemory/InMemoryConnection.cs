using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using StrawberryShake.Properties;
using StrawberryShake.Transport.InMemory;

namespace StrawberryShake.Transport.Http
{
    public class InMemoryConnection : IConnection<JsonDocument>
    {
        private readonly Func<CancellationToken, ValueTask<IInMemoryClient>> _createClientAsync;

        public InMemoryConnection(
            Func<CancellationToken, ValueTask<IInMemoryClient>> createClientAsync)
        {
            _createClientAsync = createClientAsync ??
                throw new ArgumentNullException(nameof(createClientAsync));
        }

        public async IAsyncEnumerable<Response<JsonDocument>> ExecuteAsync(
            OperationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            IInMemoryClient client = await _createClientAsync(cancellationToken);

            Exception? exception = null;
            IExecutionResult? result = null;
            try
            {
                result = await client.ExecuteAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception is not null || result is null)
            {
                yield return new Response<JsonDocument>(null, exception);
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
            switch (executionResult)
            {
                case IQueryResult queryResult:
                    yield return new Response<JsonDocument>(
                        JsonDocument.Parse(await queryResult.ToJsonAsync()),
                        null);
                    break;
                case SubscriptionResult streamResult:
                    await foreach (var result in streamResult.ReadResultsAsync()
                        .WithCancellation(cancellationToken))
                    {
                        await foreach (var response in ProcessResultAsync(result, cancellationToken)
                            .ConfigureAwait(false))
                        {
                            yield return response;
                        }
                    }

                    break;
                default:
                    yield return new Response<JsonDocument>(
                        null,
                        new GraphQLClientException(
                            Resources.InMemoryConnection_InvalidResponseFormat));
                    yield break;
            }
        }
    }
}
