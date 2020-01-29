using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.WebSockets
{
    internal sealed class WebSocketConnectionPool
        : ISocketConnectionPool
    {
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, ConnectionInfo> _connections =
            new Dictionary<string, ConnectionInfo>();
        private readonly IWebSocketClientFactory _webSocketClientFactory;
        private readonly ISocketConnectionInterceptor[] _connectionInterceptors;
        private bool _disposed;

        public WebSocketConnectionPool(
            IWebSocketClientFactory webSocketClientFactory,
            IEnumerable<ISocketConnectionInterceptor> connectionInterceptors)
        {
            _webSocketClientFactory = webSocketClientFactory
                ?? throw new ArgumentNullException(nameof(webSocketClientFactory));
            _connectionInterceptors = connectionInterceptors?.ToArray()
                ?? throw new ArgumentNullException(nameof(connectionInterceptors));
        }

        public async Task<ISocketConnection> RentAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_connections.TryGetValue(name, out ConnectionInfo? connectionInfo))
                {
                    connectionInfo.Rentals++;
                }
                else
                {
                    connectionInfo = new ConnectionInfo(
                        new WebSocketConnection(
                            name,
                            _webSocketClientFactory.CreateClient(name)));

                    await connectionInfo.Connection.OpenAsync(cancellationToken)
                        .ConfigureAwait(false);

                    await InitializeConnectionAsync(connectionInfo.Connection, cancellationToken)
                        .ConfigureAwait(false);

                    for (int i = 0; i < _connectionInterceptors.Length; i++)
                    {
                        await _connectionInterceptors[i].OnConnectAsync(
                            connectionInfo.Connection)
                            .ConfigureAwait(false);
                    }

                    _connections.Add(name, connectionInfo);
                }

                return connectionInfo.Connection;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task ReturnAsync(
            ISocketConnection connection,
            CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_connections.TryGetValue(connection.Name, out ConnectionInfo? connectionInfo))
                {
                    connectionInfo.Rentals--;
                }
                else
                {
                    throw new ArgumentException(
                        "The specified connection does not belong to this pool.",
                        nameof(connection));
                }

                if (connectionInfo.Rentals < 1)
                {
                    try
                    {
                        for (int i = 0; i < _connectionInterceptors.Length; i++)
                        {
                            await _connectionInterceptors[i].OnDisconnectAsync(
                                connectionInfo.Connection)
                                .ConfigureAwait(false);
                        }
                        await TerminateConnectionAsync(connection, cancellationToken)
                            .ConfigureAwait(false);
                        _connections.Remove(connection.Name);
                        await connection.CloseAsync(
                                "All subscriptions closed.",
                                SocketCloseStatus.NormalClosure,
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // we ignore errors here
                    }
                    finally
                    {
                        connection.Dispose();
                    }
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private Task InitializeConnectionAsync(
            ISocketConnection connection,
            CancellationToken cancellationToken = default)
        {
            var messageWriter = new SocketMessageWriter();
            messageWriter.WriteStartObject();
            messageWriter.WriteType(MessageTypes.Connection.Initialize);
            messageWriter.WriteEndObject();

            return connection.SendAsync(messageWriter.Body, cancellationToken);
        }

        private Task TerminateConnectionAsync(
            ISocketConnection connection,
            CancellationToken cancellationToken = default)
        {
            var messageWriter = new SocketMessageWriter();
            messageWriter.WriteStartObject();
            messageWriter.WriteType(MessageTypes.Connection.Terminate);
            messageWriter.WriteEndObject();

            return connection.SendAsync(messageWriter.Body, cancellationToken);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (ConnectionInfo connection in _connections.Values)
                {
                    connection.Connection.Dispose();
                }
                _connections.Clear();
                _disposed = true;
            }
        }

        private sealed class ConnectionInfo
        {
            public ConnectionInfo(ISocketConnection connection)
            {
                Connection = connection;
                Rentals = 1;
            }

            public ISocketConnection Connection { get; }

            public int Rentals { get; set; }
        }
    }
}
