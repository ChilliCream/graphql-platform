using System.Net.WebSockets;

namespace HotChocolate.Transport.Sockets.Client.Protocols;

/// <summary>
/// Represents the context for a WebSocket client.
/// </summary>
internal sealed class SocketClientContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SocketClientContext"/> class with
    /// the specified WebSocket object.
    /// </summary>
    /// <param name="socket">
    /// The <see cref="WebSocket"/> object representing the WebSocket connection.
    /// </param>
    public SocketClientContext(WebSocket socket)
    {
        Socket = socket;
        Messages = new MessageStream();
    }

    /// <summary>
    /// Gets the <see cref="WebSocket"/> object representing the WebSocket connection.
    /// </summary>
    public WebSocket Socket { get; }

    /// <summary>
    /// Gets the <see cref="MessageStream"/> object representing the message stream
    /// for the WebSocket connection.
    /// </summary>
    public MessageStream Messages { get; }
}
