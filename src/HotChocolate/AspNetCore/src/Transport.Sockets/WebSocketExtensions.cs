using System.Net.WebSockets;

namespace HotChocolate.Transport.Sockets;

public static class WebSocketExtensions
{
    public static bool IsOpen(this WebSocket? webSocket)
    {
        if (webSocket is null)
        {
            return false;
        }

        return webSocket.State.HasFlag(WebSocketState.Open);
    }

    public static bool IsClosed(this WebSocket? webSocket)
        => !IsOpen(webSocket);
}
