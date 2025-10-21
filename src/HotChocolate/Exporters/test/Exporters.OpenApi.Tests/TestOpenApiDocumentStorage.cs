using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class TestOpenApiDocumentStorage : IOpenApiDocumentStorage, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(initialCount: 1, maxCount: 1);
    private readonly Dictionary<string, OpenApiDocumentDefinition> _documentsById = [];
    private ImmutableList<ObserverSession> _sessions = [];
    private bool _disposed;
    private readonly object _sync = new();

    public async ValueTask<IEnumerable<OpenApiDocumentDefinition>> GetDocumentsAsync(
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return _documentsById.Values.ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public IDisposable Subscribe(IObserver<OpenApiDocumentStorageEventArgs> observer)
    {
        return new ObserverSession(this, observer);
    }

    public async Task AddOrUpdateDocumentAsync(
        string id,
        DocumentNode document,
        CancellationToken cancellationToken = default)
    {
        var operation = document.Definitions.OfType<OperationDefinitionNode>().FirstOrDefault();

        if (operation is null)
        {
            throw new ArgumentException($"Document {document} has no operation definition");
        }

        OpenApiDocumentStorageEventType type;
        await _semaphore.WaitAsync(cancellationToken);

        OpenApiDocumentDefinition tool;
        try
        {
            tool = new OpenApiDocumentDefinition(id, document);
            if (_documentsById.TryAdd(id, tool))
            {
                type = OpenApiDocumentStorageEventType.Added;
            }
            else
            {
                _documentsById[id] = tool;
                type = OpenApiDocumentStorageEventType.Modified;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        NotifySubscribers(id, tool, type);
    }

    public async Task RemoveDocumentAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        bool removed;
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            removed = _documentsById.Remove(id);
        }
        finally
        {
            _semaphore.Release();
        }

        if (removed)
        {
            NotifySubscribers(id, null, OpenApiDocumentStorageEventType.Removed);
        }
    }

    private void NotifySubscribers(
        string id,
        OpenApiDocumentDefinition? toolDefinition,
        OpenApiDocumentStorageEventType type)
    {
        if (type is OpenApiDocumentStorageEventType.Added or OpenApiDocumentStorageEventType.Modified)
        {
            ArgumentNullException.ThrowIfNull(toolDefinition);
        }

        if (_disposed)
        {
            return;
        }

        var sessions = _sessions;
        var eventArgs = new OpenApiDocumentStorageEventArgs(id, type, toolDefinition);

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
        private readonly TestOpenApiDocumentStorage _storage;
        private readonly IObserver<OpenApiDocumentStorageEventArgs> _observer;

        public ObserverSession(
            TestOpenApiDocumentStorage storage,
            IObserver<OpenApiDocumentStorageEventArgs> observer)
        {
            _storage = storage;
            _observer = observer;

            lock (storage._sync)
            {
                _storage._sessions = _storage._sessions.Add(this);
            }
        }

        public void Notify(OpenApiDocumentStorageEventArgs eventArgs)
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
