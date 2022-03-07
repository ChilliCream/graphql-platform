using System.Collections;
using System.Collections.Concurrent;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore.Subscriptions;

public sealed class OperationManager : IOperationManager
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<string, IOperationSession> _subs = new();
    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _cancellationToken;
    private readonly ISocketSession _socketSession;
    private readonly ISocketSessionInterceptor _interceptor;
    private readonly IErrorHandler _errorHandler;
    private readonly IRequestExecutor _executor;
    private readonly Func<string, IOperationSession> _createSession;
    private bool _disposed;

    public OperationManager(
        ISocketSession socketSession,
        ISocketSessionInterceptor interceptor,
        IRequestExecutor executor)
    {
        _socketSession = socketSession ?? throw new ArgumentNullException(nameof(socketSession));
        _interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _createSession = CreateSession;
        _errorHandler = executor.Services.GetRequiredService<IErrorHandler>();
        _cts = new CancellationTokenSource();
        _cancellationToken = _cts.Token;
    }

    public OperationManager(
        ISocketSession socketSession,
        ISocketSessionInterceptor interceptor,
        IRequestExecutor executor,
        Func<string, IOperationSession> createSession)
    {
        _socketSession = socketSession ?? throw new ArgumentNullException(nameof(socketSession));
        _interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _createSession = createSession ?? throw new ArgumentNullException(nameof(createSession));
        _errorHandler = executor.Services.GetRequiredService<IErrorHandler>();
        _cts = new CancellationTokenSource();
        _cancellationToken = _cts.Token;
    }

    public bool Register(string sessionId, GraphQLRequest request)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            throw new ArgumentException(
                OperationManager_Register_SessionIdNullOrEmpty,
                nameof(sessionId));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(OperationManager));
        }

        IOperationSession? session = null;
        _lock.EnterWriteLock();

        try
        {
            if(!_subs.ContainsKey(sessionId))
            {
                session = CreateSession(sessionId);
                _subs.Add(sessionId, session);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        if (session is not null)
        {
            session.Completed += (_, _) => Unregister(sessionId);
            session.BeginExecute(request, _cancellationToken);
            return true;
        }

        return false;
    }

    public bool Unregister(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            throw new ArgumentException(
                OperationManager_Register_SessionIdNullOrEmpty,
                nameof(sessionId));
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(OperationManager));
        }

        _lock.EnterWriteLock();

        try
        {
            if (_subs.TryGetValue(sessionId, out IOperationSession? session))
            {
                _subs.Remove(sessionId);
                session.Dispose();
                return true;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return false;
    }

    private OperationSession CreateSession(string sessionId)
        => new(_socketSession, _interceptor, _errorHandler, _executor, sessionId);

    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            _cts.Dispose();
            _subs.Clear();
            _disposed = true;
        }
    }

    public IEnumerator<IOperationSession> GetEnumerator()
    {
        _lock.EnterReadLock();
        IOperationSession[] items;

        try
        {
            items = _subs.Values.ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }

        foreach (IOperationSession session in items)
        {
            yield return session;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
