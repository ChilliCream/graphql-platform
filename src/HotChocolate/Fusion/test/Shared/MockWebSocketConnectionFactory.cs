using System.Net.WebSockets;
using HotChocolate.Fusion.Clients;
using Microsoft.AspNetCore.TestHost;
using static HotChocolate.Transport.Sockets.WellKnownProtocols;

namespace HotChocolate.Fusion.Shared;

public class MockWebSocketConnectionFactory(
    Dictionary<string, Func<IWebSocketConnection>> clients)
    : IWebSocketConnectionFactory
{
    public IWebSocketConnection CreateConnection(string name)
        => clients[name].Invoke();
}

public sealed class MockWebSocketConnection : IWebSocketConnection
{
    private readonly WebSocketClient _client;

    public MockWebSocketConnection(WebSocketClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));

        _client.ConfigureRequest =
            r => r.Headers.SecWebSocketProtocol = GraphQL_Transport_WS;
    }

    public WebSocket? WebSocket { get; private set; }

    public async ValueTask<WebSocket> ConnectAsync(
        Uri uri,
        string subProtocol,
        CancellationToken cancellationToken = default)
        => WebSocket = await _client.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);

    public void Dispose()
        => WebSocket?.Dispose();
}
