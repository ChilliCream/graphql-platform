using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Properties;

namespace StrawberryShake.Transport.WebSockets
{
    /// <inheritdoc />
    internal sealed class SocketClientPool
        : ISocketClientPool
    {
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
        private readonly Dictionary<string, ClientInfo> _clients = new();
        private readonly ISocketClientFactory _socketClientFactory;

        private bool _disposed;

        public SocketClientPool(
            ISocketClientFactory socketClientFactory)
        {
            _socketClientFactory = socketClientFactory
                ?? throw new ArgumentNullException(nameof(socketClientFactory));
        }

        /// <inheritdoc />
        public async Task<ISocketClient> RentAsync(
            string name,
            CancellationToken cancellationToken = default)
            {
            await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_clients.TryGetValue(name, out ClientInfo? connectionInfo))
                {
                    connectionInfo.Rentals++;
                }
                else
                {
                    connectionInfo = new ClientInfo(_socketClientFactory.CreateClient(name));

                    await connectionInfo.Client.OpenAsync(cancellationToken)
                        .ConfigureAwait(false);

                    _clients.Add(name, connectionInfo);
                }

                return connectionInfo.Client;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <inheritdoc />
        public async Task ReturnAsync(
            ISocketClient client,
            CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_clients.TryGetValue(client.Name, out ClientInfo? connectionInfo))
                {
                    connectionInfo.Rentals--;
                }
                else
                {
                    throw ThrowHelper.SocketClientPool_ClientNotFromPool(nameof(client));
                }

                if (connectionInfo.Rentals < 1)
                {
                    try
                    {
                        _clients.Remove(client.Name);
                        await client.CloseAsync(
                                Resources.SocketClient_AllOperationsFinished,
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
                        await client.DisposeAsync();
                    }
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                foreach (ClientInfo connection in _clients.Values)
                {
                    await connection.Client.DisposeAsync();
                }
                _clients.Clear();
                _disposed = true;
            }
        }

        private sealed class ClientInfo
        {
            public ClientInfo(ISocketClient connection)
            {
                Client = connection;
                Rentals = 1;
            }

            public ISocketClient Client { get; }

            public int Rentals { get; set; }
        }
    }
}
