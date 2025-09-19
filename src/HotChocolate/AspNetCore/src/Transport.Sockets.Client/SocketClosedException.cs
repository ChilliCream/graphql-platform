using System.Net.WebSockets;

namespace HotChocolate.Transport.Sockets.Client;

public sealed class SocketClosedException : Exception
{
    public SocketClosedException(string? message, WebSocketCloseStatus reason) : base(message)
    {
        Reason = reason;
    }

    public WebSocketCloseStatus Reason { get; }
}
