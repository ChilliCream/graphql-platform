using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Transport.Sockets.Client;
using static HotChocolate.Stitching.WellKnownContextData;

namespace HotChocolate.Stitching.Execution;

internal sealed class SubscriptionRequestHandler : IRemoteRequestHandler
{
    private readonly ISocketClientFactory _clientFactory;
    private readonly NameString _targetSchema;

    public SubscriptionRequestHandler(
        ISocketClientFactory clientFactory,
        NameString targetSchema)
    {
        _clientFactory = clientFactory;
        _targetSchema = targetSchema;
    }

    public bool CanHandle(IQueryRequest request)
        => request.ContextData?.ContainsKey(IsSubscription) ?? false;

    public Task<IExecutionResult> ExecuteAsync(
        IQueryRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IExecutionResult>(
            new ResponseStream(
                () => new Subscription(_clientFactory, _targetSchema, CreateRequest(request))));

    private static OperationRequest CreateRequest(IQueryRequest queryRequest)
        => new(queryRequest.Query!.ToString(),
            variables: queryRequest.VariableValues,
            extensions: queryRequest.Extensions);

    private sealed class Subscription : IAsyncEnumerable<IQueryResult>
    {
        private readonly ISocketClientFactory _clientFactory;
        private readonly NameString _targetSchema;
        private readonly OperationRequest _request;

        public Subscription(
            ISocketClientFactory clientFactory,
            NameString targetSchema,
            OperationRequest request)
        {
            _clientFactory = clientFactory;
            _targetSchema = targetSchema;
            _request = request;
        }

        public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            await using SocketClient client =
                await _clientFactory.CreateClientAsync(_targetSchema, cancellationToken);

            SocketResult socketResult = await client.ExecuteAsync(_request, cancellationToken);

            await foreach (OperationResult payload in
                socketResult.ReadResultsAsync().WithCancellation(cancellationToken))
            {
                yield return JsonResponseDeserializer.Deserialize(payload);
            }
        }
    }
}
