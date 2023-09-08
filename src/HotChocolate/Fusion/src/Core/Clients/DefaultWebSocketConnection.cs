using System.Net.WebSockets;
using HotChocolate.Transport.Sockets;

namespace HotChocolate.Fusion.Clients;

/// <summary>
/// Represents a WebSocket connection.
/// </summary>
internal sealed class DefaultWebSocketConnection : IWebSocketConnection
{
    /// <summary>
    /// Get the underlying <see cref="ClientWebSocket"/> instance.
    /// </summary>
    public ClientWebSocket? WebSocket { get; private set; }

    /// <summary>
    /// Asynchronously connects to the specified WebSocket URI.
    /// </summary>
    /// <param name="uri">
    /// The URI of the WebSocket server to connect to.
    /// </param>
    /// <param name="subProtocol">
    /// The sub-protocol to use for the connection.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation.
    /// </returns>
    public async ValueTask<WebSocket> ConnectAsync(
        Uri uri,
        string subProtocol,
        CancellationToken cancellationToken = default)
    {
        if (WebSocket is not null)
        {
            return WebSocket;
        }

        WebSocket = new ClientWebSocket();
        WebSocket.Options.AddSubProtocol(WellKnownProtocols.GraphQL_Transport_WS);
        await WebSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        return WebSocket;
    }

    public void Dispose() => WebSocket?.Dispose();
}
