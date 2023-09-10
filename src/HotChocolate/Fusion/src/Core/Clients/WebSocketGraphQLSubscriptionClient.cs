using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Transport;
using HotChocolate.Transport.Sockets;
using HotChocolate.Transport.Sockets.Client;

namespace HotChocolate.Fusion.Clients;

public abstract class WebSocketGraphQLSubscriptionClient : IGraphQLSubscriptionClient
{
    private readonly WebSocketClientConfiguration _configuration;
    private readonly IWebSocketConnection _connection;

    protected WebSocketGraphQLSubscriptionClient(
        WebSocketClientConfiguration configuration,
        IWebSocketConnection connection)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public string SubgraphName => _configuration.SubgraphName;

    public IAsyncEnumerable<GraphQLResponse> SubscribeAsync(
        SubgraphGraphQLRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return SubscribeInternalAsync(request, cancellationToken);
    }

    private async IAsyncEnumerable<GraphQLResponse> SubscribeInternalAsync(
        SubgraphGraphQLRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var socket = await _connection.ConnectAsync(
                _configuration.EndpointUri,
                WellKnownProtocols.GraphQL_Transport_WS,
                ct)
            .ConfigureAwait(false);

        try
        {
            var operationRequest = new OperationRequest(
                query: request.Document,
                id: null,
                operationName: null,
                variables: request.VariableValues,
                extensions: request.Extensions);

            await using var client = await ConnectAsync(request, socket, ct)
                .ConfigureAwait(false);
            using var socketResult = await client.ExecuteAsync(operationRequest, ct)
                .ConfigureAwait(false);

            await foreach (var operationResult in socketResult.ReadResultsAsync()
                .WithCancellation(ct).ConfigureAwait(false))
            {
                yield return new GraphQLResponse(operationResult);
            }
        }
        finally
        {
            try
            {
                await CloseWebSocketAsync(
                        socket,
                        WebSocketCloseStatus.NormalClosure,
                        "Completed",
                        ct)
                    .ConfigureAwait(false);
            }
            catch
            {
                // we will try to close the socket but if this fails we will ignore it.
            }
        }
    }

    protected virtual ValueTask<SocketClient> ConnectAsync(
        SubgraphGraphQLRequest request,
        WebSocket webSocket,
        CancellationToken cancellationToken)
        => SocketClient.ConnectAsync(webSocket, cancellationToken);

    private static async Task CloseWebSocketAsync(
        WebSocket webSocket,
        WebSocketCloseStatus closeStatus,
        string closeDescription,
        CancellationToken ct)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseOutputAsync(closeStatus, closeDescription, ct);

            await Task.Delay(50, ct);

            if (webSocket.State is WebSocketState.Open or WebSocketState.CloseSent)
            {
                await webSocket.CloseAsync(closeStatus, closeDescription, ct);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _connection.Dispose();
        return default;
    }
}
