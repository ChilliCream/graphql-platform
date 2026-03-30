using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Channels;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Connectors.InMemory;

/// <summary>
/// A fusion configuration provider that composes schemas from in-memory
/// request executors and recomposes whenever any of the source schemas change.
/// </summary>
public sealed class InMemoryConfigurationProvider : IFusionConfigurationProvider
{
#if NET9_0_OR_GREATER
    private readonly Lock _syncRoot = new();
#else
    private readonly object _syncRoot = new();
#endif
    private readonly string[] _schemaNames;
    private readonly RequestExecutorProxy[] _proxies;
    private readonly IDisposable? _eventSubscription;
    private readonly CancellationTokenSource _cts = new();

    private readonly Channel<bool> _channel =
        Channel.CreateBounded<bool>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropNewest,
                SingleReader = true,
                SingleWriter = false
            });

    private ImmutableArray<ObserverSession> _sessions = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryConfigurationProvider"/>.
    /// </summary>
    /// <param name="schemaNames">The names of the source schemas to compose.</param>
    /// <param name="executorProvider">The request executor provider.</param>
    /// <param name="executorEvents">The request executor events.</param>
    public InMemoryConfigurationProvider(
        string[] schemaNames,
        IRequestExecutorProvider executorProvider,
        IRequestExecutorEvents executorEvents)
    {
        ArgumentNullException.ThrowIfNull(schemaNames);
        ArgumentNullException.ThrowIfNull(executorProvider);
        ArgumentNullException.ThrowIfNull(executorEvents);

        _schemaNames = schemaNames;

        _proxies = new RequestExecutorProxy[schemaNames.Length];
        for (var i = 0; i < schemaNames.Length; i++)
        {
            _proxies[i] = new RequestExecutorProxy(executorProvider, executorEvents, schemaNames[i]);
        }

        var observer = new RequestExecutorEventObserver(OnExecutorEvent);
        _eventSubscription = executorEvents.Subscribe(observer);

        _channel.Writer.TryWrite(true);
        ComposeLoopAsync(_cts.Token).FireAndForget();
    }

    /// <inheritdoc />
    public FusionConfiguration? Configuration { get; private set; }

    /// <inheritdoc />
    public IDisposable Subscribe(IObserver<FusionConfiguration> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var session = new ObserverSession(this, observer);

        lock (_syncRoot)
        {
            _sessions = _sessions.Add(session);
        }

        var configuration = Configuration;

        if (configuration is not null)
        {
            observer.OnNext(configuration);
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

    private void OnExecutorEvent(RequestExecutorEvent eventArgs)
    {
        if (_disposed || eventArgs.Type is not RequestExecutorEventType.Created)
        {
            return;
        }

        for (var i = 0; i < _schemaNames.Length; i++)
        {
            if (eventArgs.Name.Equals(_schemaNames[i], StringComparison.Ordinal))
            {
                _channel.Writer.TryWrite(true);
                return;
            }
        }
    }

    private async Task ComposeLoopAsync(CancellationToken ct)
    {
        var defaultSettings = JsonDocument.Parse("{ }");

        await foreach (var _ in _channel.Reader.ReadAllAsync(ct))
        {
            try
            {
                var sourceSchemas = new SourceSchemaText[_schemaNames.Length];

                for (var i = 0; i < _proxies.Length; i++)
                {
                    var executor = await _proxies[i].GetExecutorAsync(ct).ConfigureAwait(false);
                    var sdl = SchemaPrinter.Print((Schema)executor.Schema);
                    sourceSchemas[i] = new SourceSchemaText(_schemaNames[i], sdl);
                }

                var compositionLog = new CompositionLog();
                var result = new SchemaComposer(
                    sourceSchemas,
                    new SchemaComposerOptions(),
                    compositionLog).Compose();

                if (result.IsFailure)
                {
                    NotifyError(new SchemaCompositionException(compositionLog));
                    return;
                }

                var documentNode = result.Value.ToSyntaxNode();
                var settings = new JsonDocumentOwner(defaultSettings, EmptyMemoryOwner.Instance);
                NotifyObservers(new FusionConfiguration(documentNode, settings));
            }
            catch (Exception ex)
            {
                NotifyError(ex);
                return;
            }
        }
    }

    private void NotifyObservers(FusionConfiguration configuration)
    {
        ImmutableArray<ObserverSession> sessions;

        lock (_syncRoot)
        {
            sessions = _sessions;
            Configuration = configuration;
        }

        if (sessions.IsEmpty)
        {
            return;
        }

        foreach (var session in sessions)
        {
            session.Notify(configuration);
        }
    }

    private void NotifyError(Exception exception)
    {
        ImmutableArray<ObserverSession> sessions;

        lock (_syncRoot)
        {
            sessions = _sessions;
        }

        foreach (var session in sessions)
        {
            session.Error(exception);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        _cts.Cancel();
        _cts.Dispose();
        _eventSubscription?.Dispose();

        foreach (var proxy in _proxies)
        {
            proxy.Dispose();
        }

        foreach (var session in _sessions)
        {
            session.Complete();
        }

        // drain events
        while (_channel.Reader.TryRead(out _))
        {
        }

        return ValueTask.CompletedTask;
    }

    private sealed class ObserverSession(
        InMemoryConfigurationProvider provider,
        IObserver<FusionConfiguration> observer)
        : IDisposable
    {
        public void Notify(FusionConfiguration schemaDocument)
            => observer.OnNext(schemaDocument);

        public void Error(Exception exception)
            => observer.OnError(exception);

        public void Complete()
            => observer.OnCompleted();

        public void Dispose()
            => provider.Unsubscribe(this);
    }

    private sealed class EmptyMemoryOwner : IMemoryOwner<byte>
    {
        public static readonly EmptyMemoryOwner Instance = new();

        public Memory<byte> Memory => default;

        public void Dispose() { }
    }
}
