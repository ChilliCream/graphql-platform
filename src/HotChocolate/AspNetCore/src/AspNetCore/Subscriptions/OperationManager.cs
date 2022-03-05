using System.Collections;
using System.Collections.Concurrent;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore.Subscriptions;

public sealed class OperationManager : IOperationManager
{
    private readonly ConcurrentDictionary<string, IOperationSession> _subs = new();
    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _cancellationToken;
    private readonly ISocketSession _socketSession;
    private readonly ISocketSessionInterceptor _interceptor;
    private readonly IErrorHandler _errorHandler;
    private readonly IRequestExecutor _executor;
    private bool _disposed;

    public OperationManager(
        ISocketSession socketSession,
        ISocketSessionInterceptor interceptor,
        IRequestExecutor executor)
    {
        _socketSession = socketSession;
        _interceptor = interceptor;
        _executor = executor;
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

        var registered = false;
        IOperationSession session = _subs.GetOrAdd(sessionId, CreateSession);

        if (registered)
        {
            session.Completed += (_, _) => Unregister(sessionId);
            session.BeginExecute(request, _cancellationToken);
            return true;
        }

        return false;

        IOperationSession CreateSession(string key)
        {
            registered = true;
            return new OperationSession(
                _socketSession,
                _interceptor,
                _errorHandler,
                _executor,
                key);
        }
    }

    public bool Unregister(string sessionId)
    {
        if (sessionId == null)
        {
            throw new ArgumentNullException(nameof(sessionId));
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(OperationManager));
        }

        return _subs.TryRemove(sessionId, out _);
    }

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
        => _subs.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
