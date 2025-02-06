using System.Collections;
using System.Collections.Concurrent;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore.Subscriptions;

/// <summary>
/// The operation manager provides access to registered running operation within a socket session.
/// The operation manager ensures that operation are correctly tracked and cleaned up after they
/// have been completed.
/// </summary>
public sealed class OperationManager : IOperationManager
{
    private readonly ConcurrentDictionary<string, IOperationSession> _subs = new();
    private readonly OperationSessionCompletionHandler _completion;
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
        _completion = new OperationSessionCompletionHandler(_subs);
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
        _errorHandler = executor.Services.GetRequiredService<IErrorHandler>();
        _cts = new CancellationTokenSource();
        _cancellationToken = _cts.Token;
        _completion = new OperationSessionCompletionHandler(_subs);
    }

    /// <inheritdoc />
    public bool Start(string sessionId, GraphQLRequest request)
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

        var context = new StartSessionContext(
            _createSession,
            _completion,
            request,
            _cancellationToken);

        _subs.GetOrAdd(
            sessionId,
            static (key, ctx) => ctx.CreateSession(key),
            context);

        return context.IsNewSession;
    }

    /// <inheritdoc />
    public bool Complete(string sessionId)
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

        if(_subs.TryRemove(sessionId, out var session))
        {
            session.Dispose();
            return true;
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
        => _subs.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class StartSessionContext(
        Func<string, IOperationSession> createSession,
        IOperationSessionCompletionHandler completion,
        GraphQLRequest request,
        CancellationToken cancellationToken)
    {
        public bool IsNewSession { get; private set; }

        public IOperationSession CreateSession(string sessionId)
        {
            IsNewSession = true;
            var session = createSession(sessionId);
            session.BeginExecute(request, completion, cancellationToken);
            return session;
        }
    }

    private sealed class OperationSessionCompletionHandler(
        ConcurrentDictionary<string, IOperationSession> subs)
        : IOperationSessionCompletionHandler
    {
        public void Complete(IOperationSession session)
        {
            if (subs.TryRemove(session.Id, out _))
            {
                session.Dispose();
            }
        }
    }
}
