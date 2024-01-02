using System.Net.WebSockets;
using HotChocolate.Fusion.Clients;
using Microsoft.AspNetCore.TestHost;
using static HotChocolate.Transport.Sockets.WellKnownProtocols;

namespace HotChocolate.Fusion.Shared;

public class MockWebSocketConnectionFactory : IWebSocketConnectionFactory
{
    private readonly Dictionary<string, Func<IWebSocketConnection>> _clients;

    public MockWebSocketConnectionFactory(Dictionary<string, Func<IWebSocketConnection>> clients)
        => _clients = clients;

    public IWebSocketConnection CreateConnection(string name)
        => _clients[name].Invoke();
}

public sealed class MockWebSocketConnection : IWebSocketConnection
{
    private readonly WebSocketClient _client;

    public MockWebSocketConnection(WebSocketClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));

        _client.ConfigureRequest =
            r => r.Headers.Add("Sec-WebSocket-Protocol", GraphQL_Transport_WS);
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
