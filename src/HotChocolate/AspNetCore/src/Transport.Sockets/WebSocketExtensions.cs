using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets;
#else
namespace HotChocolate.Transport.Sockets;
#endif

public static class WebSocketExtensions
{
    public static bool IsOpen([NotNullWhen(true)] this WebSocket? webSocket)
        => webSocket?.State is WebSocketState.Open;

    public static bool IsClosed([NotNullWhen(false)] this WebSocket? webSocket)
        => !IsOpen(webSocket);
}
