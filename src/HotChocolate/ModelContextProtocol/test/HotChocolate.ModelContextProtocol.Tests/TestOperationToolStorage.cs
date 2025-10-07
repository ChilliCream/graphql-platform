using System.Collections.Immutable;
using CaseConverter;
using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Storage;

namespace HotChocolate.ModelContextProtocol;

public sealed class TestOperationToolStorage : IOperationToolStorage, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(initialCount: 1, maxCount: 1);
    private readonly Dictionary<string, OperationToolDefinition> _tools = [];
    private ImmutableList<ObserverSession> _sessions = [];
    private bool _disposed;
    private readonly object _sync = new();

    public async ValueTask<IEnumerable<OperationToolDefinition>> GetToolsAsync(
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return _tools.Values.ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AddOrUpdateToolAsync(
        DocumentNode document,
        CancellationToken cancellationToken = default)
    {
        var operation = document.Definitions.OfType<OperationDefinitionNode>().FirstOrDefault();

        if (operation is null)
        {
            throw new ArgumentException($"Document {document} has no operation definition");
        }

        var name = operation.Name?.Value.ToSnakeCase()!;

        OperationToolStorageEventType type;
        await _semaphore.WaitAsync(cancellationToken);

        OperationToolDefinition tool;
        try
        {
            tool = new OperationToolDefinition(name, document);
            if (_tools.TryAdd(name, tool))
            {
                type = OperationToolStorageEventType.Added;
            }
            else
            {
                _tools[name] = tool;
                type = OperationToolStorageEventType.Modified;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        NotifySubscribers(name, tool, type);
    }

    public async Task RemoveToolAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        bool removed;

        try
        {
            removed = _tools.Remove(name);
        }
        finally
        {
            _semaphore.Release();
        }

        if (removed)
        {
            NotifySubscribers(name, null, OperationToolStorageEventType.Removed);
        }
    }

    public IDisposable Subscribe(IObserver<OperationToolStorageEventArgs> observer)
    {
        return new ObserverSession(this, observer);
    }

    private void NotifySubscribers(
        string name,
        OperationToolDefinition? toolDefinition,
        OperationToolStorageEventType type)
    {
        if (type is OperationToolStorageEventType.Added or OperationToolStorageEventType.Modified)
        {
            ArgumentNullException.ThrowIfNull(toolDefinition);
        }

        if (_disposed)
        {
            return;
        }

        var sessions = _sessions;
        var eventArgs = new OperationToolStorageEventArgs(name, type, toolDefinition);

        foreach (var session in sessions)
        {
            session.Notify(eventArgs);
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
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

    private sealed class ObserverSession : IDisposable
    {
        private bool _disposed;
        private readonly TestOperationToolStorage _storage;
        private readonly IObserver<OperationToolStorageEventArgs> _observer;

        public ObserverSession(
            TestOperationToolStorage storage,
            IObserver<OperationToolStorageEventArgs> observer)
        {
            _storage = storage;
            _observer = observer;

            lock (storage._sync)
            {
                _storage._sessions = _storage._sessions.Add(this);
            }
        }

        public void Notify(OperationToolStorageEventArgs eventArgs)
        {
            if (!_disposed && !_storage._disposed)
            {
                _observer.OnNext(eventArgs);
            }
        }

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
}
