using System.Collections.Immutable;
using HotChocolate.Adapters.Mcp.Storage;

namespace HotChocolate.Adapters.Mcp;

public sealed class TestMcpStorage : IMcpStorage, IDisposable
{
    private readonly SemaphoreSlim _promptSemaphore = new(initialCount: 1, maxCount: 1);
    private readonly SemaphoreSlim _toolSemaphore = new(initialCount: 1, maxCount: 1);
    private readonly Dictionary<string, PromptDefinition> _prompts = [];
    private readonly Dictionary<string, OperationToolDefinition> _tools = [];
    private ImmutableList<ObserverSession<PromptStorageEventArgs>> _promptSessions = [];
    private ImmutableList<ObserverSession<OperationToolStorageEventArgs>> _toolSessions = [];
    private bool _disposed;
    private readonly object _sync = new();

    public async ValueTask<IEnumerable<PromptDefinition>> GetPromptDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        await _promptSemaphore.WaitAsync(cancellationToken);

        try
        {
            return _prompts.Values.ToList();
        }
        finally
        {
            _promptSemaphore.Release();
        }
    }

    public async ValueTask<IEnumerable<OperationToolDefinition>> GetOperationToolDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        await _toolSemaphore.WaitAsync(cancellationToken);

        try
        {
            return _tools.Values.ToList();
        }
        finally
        {
            _toolSemaphore.Release();
        }
    }

    public async Task AddOrUpdatePromptAsync(
        PromptDefinition promptDefinition,
        CancellationToken cancellationToken = default)
    {
        PromptStorageEventType type;

        await _promptSemaphore.WaitAsync(cancellationToken);

        if (_prompts.TryAdd(promptDefinition.Name, promptDefinition))
        {
            type = PromptStorageEventType.Added;
        }
        else
        {
            _prompts[promptDefinition.Name] = promptDefinition;
            type = PromptStorageEventType.Modified;
        }

        _promptSemaphore.Release();

        NotifySubscribers(promptDefinition.Name, promptDefinition, type);
    }

    public async Task AddOrUpdateToolAsync(
        OperationToolDefinition toolDefinition,
        CancellationToken cancellationToken = default)
    {
        OperationToolStorageEventType type;

        await _toolSemaphore.WaitAsync(cancellationToken);

        if (_tools.TryAdd(toolDefinition.Name, toolDefinition))
        {
            type = OperationToolStorageEventType.Added;
        }
        else
        {
            _tools[toolDefinition.Name] = toolDefinition;
            type = OperationToolStorageEventType.Modified;
        }

        _toolSemaphore.Release();

        NotifySubscribers(toolDefinition.Name, toolDefinition, type);
    }

    public IDisposable Subscribe(IObserver<PromptStorageEventArgs> observer)
    {
        return new ObserverSession<PromptStorageEventArgs>(this, observer);
    }

    public IDisposable Subscribe(IObserver<OperationToolStorageEventArgs> observer)
    {
        return new ObserverSession<OperationToolStorageEventArgs>(this, observer);
    }

    private void NotifySubscribers(
        string name,
        PromptDefinition? promptDefinition,
        PromptStorageEventType type)
    {
        if (type is PromptStorageEventType.Added or PromptStorageEventType.Modified)
        {
            ArgumentNullException.ThrowIfNull(promptDefinition);
        }

        if (_disposed)
        {
            return;
        }

        var sessions = _promptSessions;
        var eventArgs = new PromptStorageEventArgs(name, type, promptDefinition);

        foreach (var session in sessions)
        {
            session.Notify(eventArgs);
        }
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

        var sessions = _toolSessions;
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
                foreach (var session in _promptSessions)
                {
                    session.Dispose();
                }

                foreach (var session in _toolSessions)
                {
                    session.Dispose();
                }

                _promptSessions = [];
                _toolSessions = [];
                _disposed = true;
            }
        }
    }

    private sealed class ObserverSession<T> : IDisposable
    {
        private bool _disposed;
        private readonly TestMcpStorage _storage;
        private readonly IObserver<T> _observer;

        public ObserverSession(TestMcpStorage storage, IObserver<T> observer)
        {
            _storage = storage;
            _observer = observer;

            lock (storage._sync)
            {
                switch (this)
                {
                    case ObserverSession<OperationToolStorageEventArgs> s:
                        _storage._toolSessions = _storage._toolSessions.Add(s);
                        break;
                    case ObserverSession<PromptStorageEventArgs> s:
                        _storage._promptSessions = _storage._promptSessions.Add(s);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported observer session type.");
                }
            }
        }

        public void Notify(T eventArgs)
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
                switch (this)
                {
                    case ObserverSession<OperationToolStorageEventArgs> s:
                        _storage._toolSessions = _storage._toolSessions.Remove(s);
                        break;
                    case ObserverSession<PromptStorageEventArgs> s:
                        _storage._promptSessions = _storage._promptSessions.Remove(s);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported observer session type.");
                }
            }

            _disposed = true;
        }
    }
}
