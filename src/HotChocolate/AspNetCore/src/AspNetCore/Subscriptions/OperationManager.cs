using System.Collections;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Subscriptions;

/// <summary>
/// The operation manager provides access to registered running operation within a socket session.
/// The operation manager ensures that operation are correctly tracked and cleaned up after they
/// have been completed.
/// </summary>
public sealed class OperationManager : IOperationManager
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<string, IOperationSession> _subs = [];
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
        _errorHandler = executor.Schema.Services.GetRequiredService<IErrorHandler>();
        _cts = new CancellationTokenSource();
        _cancellationToken = _cts.Token;
    }

    internal OperationManager(
        ISocketSession socketSession,
        ISocketSessionInterceptor interceptor,
        IRequestExecutor executor,
        Func<string, IOperationSession> createSession)
    {
        _socketSession = socketSession ?? throw new ArgumentNullException(nameof(socketSession));
        _interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _createSession = createSession ?? throw new ArgumentNullException(nameof(createSession));
        _errorHandler = executor.Schema.Services.GetRequiredService<IErrorHandler>();
        _cts = new CancellationTokenSource();
        _cancellationToken = _cts.Token;
    }

    /// <inheritdoc />
    public bool Enqueue(string sessionId, GraphQLRequest request)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        ArgumentNullException.ThrowIfNull(request);
        ObjectDisposedException.ThrowIf(_disposed, this);

        IOperationSession? session = null;
        _lock.EnterWriteLock();

        try
        {
            if(!_subs.ContainsKey(sessionId))
            {
                session = _createSession(sessionId);
                _subs.Add(sessionId, session);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        if (session is not null)
        {
            session.Completed += (_, _) => Complete(sessionId);
            session.BeginExecute(request, _cancellationToken);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool Complete(string sessionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _lock.EnterWriteLock();

        try
        {
            if (_subs.TryGetValue(sessionId, out var session))
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

        foreach (var session in items)
        {
            yield return session;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
