namespace StrawberryShake.Transport.WebSockets;

/// <inheritdoc />
internal sealed class SessionPool(ISocketClientFactory socketClientFactory) : ISessionPool
{
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly Dictionary<string, SessionInfo> _sessions = new();
    private readonly ISocketClientFactory _socketClientFactory = socketClientFactory
        ?? throw new ArgumentNullException(nameof(socketClientFactory));

    private bool _disposed;

    /// <inheritdoc />
    public async Task<ISession> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_sessions.TryGetValue(name, out var sessionInfo))
            {
                sessionInfo.Rentals++;
            }
            else
            {
                var client = _socketClientFactory.CreateClient(name);

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
            if (_sessions.TryGetValue(name, out var sessionInfo))
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
            foreach (var connection in _sessions.Values)
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
            if (_sessions.TryGetValue(session.Name, out var connectionInfo))
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

    private sealed class SessionProxy(
        Session session,
        SessionPool pool)
        : ISession
    {
        public string Name => session.Name;

        public Task OpenSessionAsync(CancellationToken cancellationToken = default)
        {
            return session.OpenSessionAsync(cancellationToken);
        }

        public Task<ISocketOperation> StartOperationAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default)
        {
            return session.StartOperationAsync(request, cancellationToken);
        }

        public Task StopOperationAsync(
            string operationId,
            CancellationToken cancellationToken = default)
        {
            return session.StopOperationAsync(operationId, cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await pool.ReturnAsync(session);
        }
    }
}
