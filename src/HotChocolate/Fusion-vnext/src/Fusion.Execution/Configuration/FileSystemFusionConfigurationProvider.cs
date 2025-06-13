using System.Collections.Immutable;
using System.IO.Hashing;
using System.IO.Pipelines;
using HotChocolate.Buffers;
using HotChocolate.Language;
using HotChocolate.Utilities;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Configuration;

public class FileSystemFusionConfigurationProvider : IFusionSchemaDocumentProvider
{
#if NET9_0_OR_GREATER
    private readonly Lock _syncRoot = new();
#else
    private readonly object _syncRoot = new();
#endif
    private readonly string _fileName;
    private readonly FileSystemWatcher _watcher;
    private readonly SemaphoreSlim _semaphore;
    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _ct;
    private ImmutableArray<ObserverSession> _sessions = [];
    private ulong _schemaDocumentHash;
    private bool _disposed;

    public FileSystemFusionConfigurationProvider(string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        var fullPath = IOPath.GetFullPath(fileName);
        var directory = IOPath.GetDirectoryName(fullPath);

        _fileName = fullPath;

        if (directory is null)
        {
            throw new FileNotFoundException("The file must contain a path.", fileName);
        }

        _semaphore = new SemaphoreSlim(1, 1);
        _cts = new CancellationTokenSource();
        _ct = _cts.Token;

        _watcher = new FileSystemWatcher
        {
            Path = directory,
            Filter = "*.*",

            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.Attributes |
                NotifyFilters.CreationTime |
                NotifyFilters.LastWrite |
                NotifyFilters.Size
        };

        _watcher.Created += (_, e) =>
        {
            if (fullPath.Equals(e.FullPath, StringComparison.Ordinal))
            {
                BeginLoadSchemaDocument();
            }
        };

        _watcher.Changed += (_, e) =>
        {
            if (fullPath.Equals(e.FullPath, StringComparison.Ordinal))
            {
                BeginLoadSchemaDocument();
            }
        };

        _watcher.EnableRaisingEvents = true;
        BeginLoadSchemaDocument();
    }

    public DocumentNode? SchemaDocument { get; private set; }

    public IDisposable Subscribe(IObserver<DocumentNode> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        var session = new ObserverSession(this, observer);

        lock (_syncRoot)
        {
            _sessions = _sessions.Add(session);
        }

        var schemaDocument = SchemaDocument;

        if (schemaDocument is not null)
        {
            observer.OnNext(SchemaDocument!);
        }

        return session;
    }

    private void Unsubscribe(ObserverSession session)
    {
        lock (_syncRoot)
        {
            _sessions = _sessions.Remove(session);
        }
    }

    private void BeginLoadSchemaDocument()
        => LoadSchemaDocumentAsync(_ct).FireAndForget();

    private async Task LoadSchemaDocumentAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var buffer = new PooledArrayWriter();
            await using var fileStream = File.OpenRead(_fileName);
            var pipeReader = PipeReader.Create(fileStream);

            while (true)
            {
                var result = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var readBuffer = result.Buffer;

                foreach (var segment in readBuffer)
                {
                    var span = segment.Span;
                    span.CopyTo(buffer.GetSpan(span.Length));
                    buffer.Advance(span.Length);
                }

                pipeReader.AdvanceTo(readBuffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            await pipeReader.CompleteAsync().ConfigureAwait(false);

            var hash = XxHash64.HashToUInt64(buffer.GetWrittenSpan());

            if (_schemaDocumentHash != hash)
            {
                _schemaDocumentHash = hash;

                var schemaDocument = Utf8GraphQLParser.Parse(buffer.GetWrittenSpan());
                SchemaDocument = schemaDocument;
                NotifyObservers(schemaDocument);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void NotifyObservers(DocumentNode schemaDocument)
    {
        var sessions = _sessions;

        if (sessions.IsEmpty)
        {
            return;
        }

        foreach (var session in sessions)
        {
            session.Notify(schemaDocument);
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();

        _cts.Cancel();

        _semaphore.Dispose();
        _cts.Dispose();

        foreach (var session in _sessions)
        {
            session.Complete();
        }

        return ValueTask.CompletedTask;
    }

    private sealed class ObserverSession : IDisposable
    {
        private readonly FileSystemFusionConfigurationProvider _provider;
        private readonly IObserver<DocumentNode> _observer;

        public ObserverSession(
            FileSystemFusionConfigurationProvider provider,
            IObserver<DocumentNode> observer)
        {
            _observer = observer;
            _provider = provider;
        }

        public void Notify(DocumentNode schemaDocument)
        {
            try
            {
                _observer.OnNext(schemaDocument);
            }
            catch (Exception ex)
            {
                _observer.OnError(ex);
            }
        }

        public void Complete()
        {
            try
            {
                _observer.OnCompleted();
            }
            catch
            {
                // We do not want to throw an exception if the observer
                // throws an exception on completion.
            }
        }

        public void Dispose()
            => _provider.Unsubscribe(this);
    }
}
