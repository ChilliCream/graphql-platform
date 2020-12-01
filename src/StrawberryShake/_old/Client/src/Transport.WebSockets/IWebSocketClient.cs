using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.WebSockets
{
    public interface IWebSocketClient
        : IDisposable
    {
        Uri? Uri { get; }

        ClientWebSocket Socket { get; }

        Task ConnectAsync(Uri? uri = default , CancellationToken cancellationToken = default);
    }
}
