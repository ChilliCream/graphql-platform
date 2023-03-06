using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using HotChocolate.Transport.Sockets.Client;

namespace HotChocolate.Fusion.Clients;

internal sealed class WebSocketGraphQLSubscriptionClient : IGraphQLSubscriptionClient
{
    private readonly Func<WebSocket> _webSocketFactory;

    public WebSocketGraphQLSubscriptionClient(
        string subgraphName,
        Func<WebSocket> webSocketFactory)
    {
        SubgraphName = subgraphName;
        _webSocketFactory = webSocketFactory;
    }

    public string SubgraphName { get; }

    public ValueTask<IAsyncEnumerable<GraphQLResponse>> SubscribeAsync(
        GraphQLRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return new(SubscribeInternalAsync(request, _webSocketFactory(), cancellationToken));
    }

    private static async IAsyncEnumerable<GraphQLResponse> SubscribeInternalAsync(
        GraphQLRequest request,
        WebSocket webSocket,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var operationRequest = new OperationRequest(
            query: request.Document.ToString(false),
            id: null,
            variables: request.VariableValues,
            extensions: request.Extensions);

        var client = await SocketClient.ConnectAsync(webSocket, ct).ConfigureAwait(false);
        using var socketResult = await client.ExecuteAsync(operationRequest, ct);

        await foreach (var operationResult in socketResult.ReadResultsAsync().WithCancellation(ct))
        {
            yield return new GraphQLResponse(operationResult);
        }
    }

    public void Dispose() { }
}
