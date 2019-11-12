using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.WebSockets
{
    internal sealed class WebSocketConnectionPool
        : ISocketConnectionPool
    {
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, WebSocketConnection> _connections =
            new ConcurrentDictionary<string, WebSocketConnection>();
        private readonly IWebSocketClientFactory _webSocketClientFactory;

        public WebSocketConnectionPool(IWebSocketClientFactory webSocketClientFactory)
        {
            _webSocketClientFactory = webSocketClientFactory
                ?? throw new ArgumentNullException(nameof(webSocketClientFactory));
        }

        public async Task<ISocketConnection> RentAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            if (!_connections.TryGetValue(name, out WebSocketConnection? connection))
            {
                await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if (!_connections.TryGetValue(name, out connection))
                    {
                        IWebSocketClient webSocketClient =
                            _webSocketClientFactory.CreateClient(name);
                        connection = new WebSocketConnection(webSocketClient);
                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        //_connections.TR(name, n =>  connection);
                    }
                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            }
        }

        public Task ReturnAsync(ISocketConnection connection, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
