using System.Net.WebSockets;

namespace HotChocolate.Transport.Sockets.Client.Protocols;

internal sealed class SocketClientContext
{
    public SocketClientContext(WebSocket socket)
    {
        Socket = socket;
        Messages = new MessageStream();
    }

    public WebSocket Socket { get; }

    public MessageStream Messages { get; }
}
