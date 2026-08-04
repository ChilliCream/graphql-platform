using System.Collections.Immutable;
using HotChocolate.Adapters.Mcp.Storage;

namespace HotChocolate.Adapters.Mcp;

/// <summary>
/// Test storage that returns the supplied definitions verbatim, allowing duplicate
/// names. This simulates a production scenario where multiple collections are
/// published to the same stage and surface overlapping definitions.
/// </summary>
internal sealed class MultiCollectionMcpStorage : IMcpStorage
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly List<PromptDefinition> _prompts;
    private readonly List<OperationToolDefinition> _tools;
    private ImmutableList<ObserverSession<PromptStorageEventArgs>> _promptSessions = [];
    private ImmutableList<ObserverSession<OperationToolStorageEventArgs>> _toolSessions = [];

    public MultiCollectionMcpStorage(
        IReadOnlyList<PromptDefinition>? prompts = null,
        IReadOnlyList<OperationToolDefinition>? tools = null)
    {
        _prompts = prompts?.ToList() ?? [];
        _tools = tools?.ToList() ?? [];
    }

    public ValueTask<IEnumerable<OperationToolDefinition>> GetOperationToolDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult<IEnumerable<OperationToolDefinition>>(_tools.ToList());
        }
    }

    public ValueTask<IEnumerable<PromptDefinition>> GetPromptDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult<IEnumerable<PromptDefinition>>(_prompts.ToList());
        }
    }

    /// <summary>
    /// Removes the prompt at the given slot and emits a Removed event. Used to simulate
    /// a collection republish where one duplicate is dropped.
    /// </summary>
    public void RemovePromptAt(int index)
    {
        PromptDefinition removed;

        lock (_sync)
        {
            removed = _prompts[index];
            _prompts.RemoveAt(index);
        }

        NotifyPrompts(new PromptStorageEventArgs(removed.Name, PromptStorageEventType.Removed, null));
    }

    /// <summary>
    /// Replaces the prompt at the given slot and emits an Updated event.
    /// </summary>
    public void ReplacePromptAt(int index, PromptDefinition definition)
    {
        lock (_sync)
        {
            _prompts[index] = definition;
        }

        NotifyPrompts(new PromptStorageEventArgs(definition.Name, PromptStorageEventType.Updated, definition));
    }

    /// <summary>
    /// Removes the tool at the given slot and emits a Removed event.
    /// </summary>
    public void RemoveToolAt(int index)
    {
        OperationToolDefinition removed;

        lock (_sync)
        {
            removed = _tools[index];
            _tools.RemoveAt(index);
        }

        NotifyTools(new OperationToolStorageEventArgs(removed.Name, OperationToolStorageEventType.Removed, null));
    }

    /// <summary>
    /// Replaces the tool at the given slot and emits an Updated event.
    /// </summary>
    public void ReplaceToolAt(int index, OperationToolDefinition definition)
    {
        lock (_sync)
        {
            _tools[index] = definition;
        }

        NotifyTools(new OperationToolStorageEventArgs(definition.Name, OperationToolStorageEventType.Updated, definition));
    }

    public IDisposable Subscribe(IObserver<OperationToolStorageEventArgs> observer)
        => new ObserverSession<OperationToolStorageEventArgs>(this, observer);

    public IDisposable Subscribe(IObserver<PromptStorageEventArgs> observer)
        => new ObserverSession<PromptStorageEventArgs>(this, observer);

    private void NotifyPrompts(PromptStorageEventArgs args)
    {
        foreach (var session in _promptSessions)
        {
            session.Notify(args);
        }
    }

    private void NotifyTools(OperationToolStorageEventArgs args)
    {
        foreach (var session in _toolSessions)
        {
            session.Notify(args);
        }
    }

    private sealed class ObserverSession<T> : IDisposable
    {
        private readonly MultiCollectionMcpStorage _storage;
        private readonly IObserver<T> _observer;
        private bool _disposed;

        public ObserverSession(MultiCollectionMcpStorage storage, IObserver<T> observer)
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
            if (!_disposed)
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
