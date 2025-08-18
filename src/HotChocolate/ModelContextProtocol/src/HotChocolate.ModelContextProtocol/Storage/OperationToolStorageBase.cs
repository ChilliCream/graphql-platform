using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.ModelContextProtocol.Storage;

/// <summary>
/// Base class for operation tool storage implementations.
/// Provides common observer pattern functionality while allowing derived classes
/// to focus on their specific storage mechanisms.
/// </summary>
public abstract class OperationToolStorageBase : IOperationToolStorage, IDisposable
{
    private readonly object _sync = new();
    private ImmutableList<ObserverSession> _sessions = [];
    private bool _disposed;

    /// <inheritdoc />
    public IDisposable Subscribe(IObserver<OperationToolStorageEventArgs> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return new ObserverSession(this, observer);
    }

    /// <inheritdoc />
    public abstract ValueTask<IEnumerable<OperationToolDefinition>> GetToolsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all subscribers of storage changes.
    /// This method is thread-safe and validates that documents are provided for add/modify operations.
    /// </summary>
    /// <param name="name">The name of the tool that changed.</param>
    /// <param name="document">The document for the tool, or null for removals.</param>
    /// <param name="type">The type of change that occurred.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when name is null, or when document is null for Add/Modified operations.
    /// </exception>
    protected void NotifySubscribers(string name, DocumentNode? document, OperationToolStorageEventType type)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (type is OperationToolStorageEventType.Added or OperationToolStorageEventType.Modified)
        {
            ArgumentNullException.ThrowIfNull(document);
        }

        if (_disposed)
        {
            return;
        }

        var sessions = _sessions;
        var eventArgs = new OperationToolStorageEventArgs(name, type, document);

        foreach (var session in sessions)
        {
            session.Notify(eventArgs);
        }
    }

    /// <summary>
    /// Manages the lifecycle of an observer subscription.
    /// Automatically removes itself from the sessions list when disposed.
    /// </summary>
    private sealed class ObserverSession : IDisposable
    {
        private bool _disposed;
        private readonly OperationToolStorageBase _storage;
        private readonly IObserver<OperationToolStorageEventArgs> _observer;

        /// <summary>
        /// Initializes a new observer session and adds it to the storage's session list.
        /// </summary>
        /// <param name="storage">The storage instance that owns this session.</param>
        /// <param name="observer">The observer to notify of storage changes.</param>
        public ObserverSession(
            OperationToolStorageBase storage,
            IObserver<OperationToolStorageEventArgs> observer)
        {
            _storage = storage;
            _observer = observer;

            lock (storage._sync)
            {
                _storage._sessions = _storage._sessions.Add(this);
            }
        }

        /// <summary>
        /// Forwards storage events to the subscribed observer if not disposed.
        /// </summary>
        /// <param name="eventArgs">The event arguments to forward.</param>
        public void Notify(OperationToolStorageEventArgs eventArgs)
        {
            if (!_disposed && !_storage._disposed)
            {
                _observer.OnNext(eventArgs);
            }
        }

        /// <summary>
        /// Removes this session from the storage's observer list.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (_storage._sync)
            {
                _storage._sessions = _storage._sessions.Remove(this);
            }

            _disposed = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    /// <summary>
    /// Releases resources used by the storage.
    /// Disposes all active observer sessions and clears the session list.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            lock (_sync)
            {
                foreach (var session in _sessions)
                {
                    session.Dispose();
                }

                _sessions = [];
                _disposed = true;
            }
        }
    }
}
