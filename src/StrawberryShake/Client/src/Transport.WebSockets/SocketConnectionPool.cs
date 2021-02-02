using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.WebSockets
{
    /// <inheritdoc />
    internal sealed class SocketSessionPool
        : ISocketSessionPool
    {
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
        private readonly Dictionary<string, SessionInfo> _sessions = new();
        private readonly ISocketClientFactory _socketClientFactory;

        private bool _disposed;

        public SocketSessionPool(ISocketClientFactory socketClientFactory)
        {
            _socketClientFactory = socketClientFactory
                ?? throw new ArgumentNullException(nameof(socketClientFactory));
        }

        /// <inheritdoc />
        public async Task<ISessionManager> RentAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_sessions.TryGetValue(name, out SessionInfo? sessionInfo))
                {
                    sessionInfo.Rentals++;
                }
                else
                {
                    ISocketClient client = _socketClientFactory.CreateClient(name);

                    var sessionManager = new SessionManager(client);
                    sessionInfo = new SessionInfo(sessionManager);
                    _sessions.Add(name, sessionInfo);

                    await sessionManager
                        .OpenSessionAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                return sessionInfo.SessionManager;
            }
            catch
            {
                if (_sessions.TryGetValue(name, out SessionInfo? sessionInfo))
                {
                    await RemoveAndDisposeAsync(
                            sessionInfo.SessionManager,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                throw;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <inheritdoc />
        public async Task ReturnAsync(
            ISessionManager sessionManager,
            CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_sessions.TryGetValue(sessionManager.Name, out SessionInfo? connectionInfo))
                {
                    connectionInfo.Rentals--;
                }
                else
                {
                    throw ThrowHelper.SocketClientPool_ClientNotFromPool(nameof(sessionManager));
                }

                if (connectionInfo.Rentals < 1)
                {
                    await RemoveAndDisposeAsync(sessionManager, cancellationToken)
                        .ConfigureAwait(false);
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
                foreach (SessionInfo connection in _sessions.Values)
                {
                    await connection.SessionManager.DisposeAsync();
                }

                _sessions.Clear();
                _disposed = true;
            }
        }

        private async Task RemoveAndDisposeAsync(
            ISessionManager sessionManager,
            CancellationToken cancellationToken)
        {
            try
            {
                _sessions.Remove(sessionManager.Name);

                await sessionManager
                    .CloseSessionAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // we ignore errors here
            }
            finally
            {
                await sessionManager.DisposeAsync();
            }
        }

        private sealed class SessionInfo
        {
            public SessionInfo(ISessionManager sessionManager)
            {
                SessionManager = sessionManager;
                Rentals = 1;
            }

            public ISessionManager SessionManager { get; }

            public int Rentals { get; set; }
        }
    }
}
