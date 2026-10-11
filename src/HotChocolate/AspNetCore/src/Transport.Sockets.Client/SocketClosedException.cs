using System.Net.WebSockets;

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client;
#else
namespace HotChocolate.Transport.Sockets.Client;
#endif

public sealed class SocketClosedException : Exception
{
    public SocketClosedException(string? message, WebSocketCloseStatus reason) : base(message)
    {
        Reason = reason;
    }

    public WebSocketCloseStatus Reason { get; }
}
