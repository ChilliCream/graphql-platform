using System.Collections.Immutable;
using HotChocolate.Adapters.Mcp.Storage;

namespace HotChocolate.Adapters.Mcp;

public sealed class TestMcpStorage : IMcpStorage, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(initialCount: 1, maxCount: 1);
    private readonly Dictionary<string, OperationToolDefinition> _tools = [];
    private ImmutableList<ObserverSession> _sessions = [];
    private bool _disposed;
    private readonly object _sync = new();

    public async ValueTask<IEnumerable<OperationToolDefinition>> GetOperationToolDefinitionsAsync(
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
        OperationToolDefinition toolDefinition,
        CancellationToken cancellationToken = default)
    {
        OperationToolStorageEventType type;

        await _semaphore.WaitAsync(cancellationToken);

        if (_tools.TryAdd(toolDefinition.Name, toolDefinition))
        {
            type = OperationToolStorageEventType.Added;
        }
        else
        {
            _tools[toolDefinition.Name] = toolDefinition;
            type = OperationToolStorageEventType.Modified;
        }

        _semaphore.Release();

        NotifySubscribers(toolDefinition.Name, toolDefinition, type);
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
        private readonly TestMcpStorage _storage;
        private readonly IObserver<OperationToolStorageEventArgs> _observer;

        public ObserverSession(
            TestMcpStorage storage,
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
