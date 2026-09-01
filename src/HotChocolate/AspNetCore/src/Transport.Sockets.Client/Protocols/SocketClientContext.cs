using System.Net.WebSockets;

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols;
#else
namespace HotChocolate.Transport.Sockets.Client.Protocols;
#endif

/// <summary>
/// Represents the context for a WebSocket client.
/// </summary>
internal sealed class SocketClientContext
{
#if FUSION
    /// <summary>
    /// Initializes a WebSocket client context with its connection options.
    /// </summary>
    /// <param name="socket">The WebSocket connection.</param>
    /// <param name="options">The options that configure the client.</param>
    public SocketClientContext(WebSocket socket, SocketClientOptions options)
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="SocketClientContext"/> class with
    /// the specified WebSocket object.
    /// </summary>
    /// <param name="socket">
    /// The <see cref="WebSocket"/> object representing the WebSocket connection.
    /// </param>
    public SocketClientContext(WebSocket socket)
#endif
    {
        Socket = socket;
        Messages = new MessageStream();
#if FUSION
        Options = options;
#endif
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

#if FUSION
    public SocketClientOptions Options { get; }
#endif
}
