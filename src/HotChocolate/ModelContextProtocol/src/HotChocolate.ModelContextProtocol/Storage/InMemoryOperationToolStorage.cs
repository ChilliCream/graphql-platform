using System.Collections.Immutable;
using CaseConverter;
using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Extensions;

namespace HotChocolate.ModelContextProtocol.Storage;

public sealed class InMemoryOperationToolStorage : IOperationToolStorage
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, OperationToolDefinition> _tools = [];
    private ImmutableList<ObserverSession> _sessions = [];

    public IDisposable Subscribe(IObserver<OperationToolStorageEventArgs> observer)
    {
        _semaphore.Wait();

        try
        {
            var session = new ObserverSession(this, observer);
            _sessions = _sessions.Add(session);
            return session;
        }
        finally
        {
            _semaphore.Release();
        }
    }

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

    public Task AddToolAsync(DocumentNode document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var operation = document.Definitions.OfType<OperationDefinitionNode>().FirstOrDefault();

        if (operation is null)
        {
            throw new ArgumentException($"Document {document} has no operation definition");
        }

        var toolDirective = operation.GetMcpToolDirective();
        var name = toolDirective?.Title;
        name ??= operation.Name?.Value.ToSnakeCase();
        return AddToolAsync(name!, document, cancellationToken);
    }

    public async Task AddToolAsync(string name, DocumentNode document, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(document);

        OperationToolStorageEventType type;
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            var tool = new OperationToolDefinition(name, document);
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

        NotifySubscribers(name, document, type);
    }

    private void NotifySubscribers(string name, DocumentNode? document, OperationToolStorageEventType type)
    {
        var sessions = _sessions;
        var eventArgs = new OperationToolStorageEventArgs(name, type, document);

        foreach (var session in sessions)
        {
            session.Notify(eventArgs);
        }
    }

    private sealed class ObserverSession(
        InMemoryOperationToolStorage storage,
        IObserver<OperationToolStorageEventArgs> observer)
        : IDisposable
    {
        private bool _disposed;

        public void Notify(OperationToolStorageEventArgs eventArgs)
        {
            observer.OnNext(eventArgs);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            storage._semaphore.Wait();

            try
            {
                storage._sessions = storage._sessions.Remove(this);
            }
            finally
            {
                storage._semaphore.Release();
            }
        }
    }
}
