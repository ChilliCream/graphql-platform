using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.WebSockets
{
    public sealed class WebSocketClient
        : IWebSocketClient
    {
        public WebSocketClient()
        {
            Socket = new ClientWebSocket();
        }

        public Uri? Uri { get; set; }

        public ClientWebSocket Socket { get; }

        public Task ConnectAsync(
            Uri? uri = default,
            CancellationToken cancellationToken = default) =>
            Socket.ConnectAsync(uri ?? Uri, cancellationToken);

        public void Dispose() => Socket.Dispose();
    }
}
