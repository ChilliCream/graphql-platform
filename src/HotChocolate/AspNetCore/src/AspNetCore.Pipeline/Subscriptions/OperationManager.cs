using System.Collections;
#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Subscriptions;

/// <summary>
/// The operation manager provides access to registered running operation within a socket session.
/// The operation manager ensures that operation are correctly tracked and cleaned up after they
/// have been completed.
/// </summary>
#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public sealed class OperationManager : IOperationManager
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<string, IOperationSession> _subs = [];
    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _cancellationToken;
    private readonly ISocketSession _socketSession;
    private readonly ExecutorSession _executorSession;
    private readonly Func<string, IOperationSession> _createSession;
    private bool _disposed;

    public OperationManager(ISocketSession socketSession, ExecutorSession executorSession)
    {
        ArgumentNullException.ThrowIfNull(socketSession);
        ArgumentNullException.ThrowIfNull(executorSession);

        _socketSession = socketSession;
        _executorSession = executorSession;

        _createSession = CreateSession;
        _cts = new CancellationTokenSource();
        _cancellationToken = _cts.Token;
        _createSession = CreateSession;
    }

    internal OperationManager(
        ISocketSession socketSession,
        ExecutorSession executorSession,
        Func<string, IOperationSession> createSession)
    {
        ArgumentNullException.ThrowIfNull(socketSession);
        ArgumentNullException.ThrowIfNull(executorSession);
        ArgumentNullException.ThrowIfNull(createSession);

        _socketSession = socketSession;
        _executorSession = executorSession;

        _createSession = CreateSession;
        _cts = new CancellationTokenSource();
        _cancellationToken = _cts.Token;
        _createSession = createSession;
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
            if (!_subs.ContainsKey(sessionId))
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
            if (_subs.Remove(sessionId, out var session))
            {
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
        => new(_socketSession, _executorSession, sessionId);

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
