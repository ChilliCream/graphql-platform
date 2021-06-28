using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.WebSockets
{
    /// <inheritdoc />
    internal sealed class SessionPool
        : ISessionPool
    {
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
        private readonly Dictionary<string, SessionInfo> _sessions = new();
        private readonly ISocketClientFactory _socketClientFactory;

        private bool _disposed;

        public SessionPool(ISocketClientFactory socketClientFactory)
        {
            _socketClientFactory = socketClientFactory
                ?? throw new ArgumentNullException(nameof(socketClientFactory));
        }

        /// <inheritdoc />
        public async Task<ISession> CreateAsync(
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

                    sessionInfo = SessionInfo.From(client, this);
                    _sessions.Add(name, sessionInfo);

                    await sessionInfo.Session
                        .OpenSessionAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                return sessionInfo.Proxy;
            }
            catch
            {
                if (_sessions.TryGetValue(name, out SessionInfo? sessionInfo))
                {
                    await RemoveAndDisposeAsync(
                            sessionInfo.Session,
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

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                foreach (SessionInfo connection in _sessions.Values)
                {
                    await connection.Session.DisposeAsync();
                }

                _sessions.Clear();
                _disposed = true;
            }
        }

        /// <summary>
        /// Returns a socket session to the pool.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="cancellationToken">The cancellation token for the operation</param>
        private async Task ReturnAsync(
            ISession session,
            CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_sessions.TryGetValue(session.Name, out SessionInfo? connectionInfo))
                {
                    connectionInfo.Rentals--;
                }
                else
                {
                    throw ThrowHelper.SocketClientPool_ClientNotFromPool(nameof(session));
                }

                if (connectionInfo.Rentals < 1)
                {
                    await RemoveAndDisposeAsync(connectionInfo.Session, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task RemoveAndDisposeAsync(
            Session session,
            CancellationToken cancellationToken)
        {
            try
            {
                _sessions.Remove(session.Name);

                await session
                    .CloseSessionAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // we ignore errors here
            }
            finally
            {
                await session.DisposeAsync();
            }
        }

        private sealed class SessionInfo
        {
            private SessionInfo(
                Session session,
                ISession proxy)
            {
                Session = session;
                Proxy = proxy;
                Rentals = 1;
            }

            public Session Session { get; }

            public ISession Proxy { get; }

            public int Rentals { get; set; }

            public static SessionInfo From(ISocketClient socketClient, SessionPool sessionPool)
            {
                var session = new Session(socketClient);
                var proxy = new SessionProxy(session, sessionPool);
                return new SessionInfo(session, proxy);
            }
        }

        private sealed class SessionProxy : ISession
        {
            private readonly Session _session;
            private SessionPool _pool;

            public SessionProxy(
                Session session,
                SessionPool pool)
            {
                _pool = pool;
                _session = session;
            }

            public string Name => _session.Name;

            public Task<ISocketOperation> StartOperationAsync(
                OperationRequest request,
                CancellationToken cancellationToken = default)
            {
                return _session.StartOperationAsync(request, cancellationToken);
            }

            public Task StopOperationAsync(
                string operationId,
                CancellationToken cancellationToken = default)
            {
                return _session.StopOperationAsync(operationId, cancellationToken);
            }

            public async ValueTask DisposeAsync()
            {
                await _pool.ReturnAsync(_session);
            }
        }
    }
}
