using System.Threading.Channels;
using HotChocolate.Features;
using HotChocolate.Utilities;

namespace HotChocolate.Execution;

/// <summary>
/// Provides a proxy for managing and executing GraphQL requests against a specific schema.
/// The <see cref="RequestExecutorProxy"/> handles the resolution, caching, and hot-swapping
/// of the underlying <see cref="IRequestExecutor"/> instance for a given schema name,
/// ensuring thread-safe access and automatic updates in response to schema changes or evictions.
/// </summary>
public class RequestExecutorProxy : IRequestExecutor, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IRequestExecutorProvider _executorProvider;
    private readonly string _schemaName;
    private readonly IDisposable? _eventSubscription;
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<RequestExecutorEvent> _events =
        Channel.CreateBounded<RequestExecutorEvent>(
            new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest
            });
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestExecutorProxy" />.
    /// </summary>
    /// <param name="executorProvider">
    /// The request executor provider.
    /// </param>
    /// <param name="executorEvents">
    /// The request executor events.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema.
    /// </param>
    public RequestExecutorProxy(
        IRequestExecutorProvider executorProvider,
        IRequestExecutorEvents executorEvents,
        string schemaName)
    {
        ArgumentNullException.ThrowIfNull(executorProvider);
        ArgumentNullException.ThrowIfNull(executorEvents);
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        _executorProvider = executorProvider;
        _schemaName = schemaName;

        var observer = new RequestExecutorEventObserver(OnRequestExecutorEvent);
        _eventSubscription = executorEvents.Subscribe(observer);
        OnUpdateRequestExecutorAsync(_cts.Token).FireAndForget();
    }

    /// <summary>
    /// Gets the name of the schema that this executor serves up.
    /// </summary>
    public string SchemaName => _schemaName;

    /// <summary>
    /// Specifies if this executor serves up the default schema.
    /// </summary>
    public bool IsDefaultSchema => _schemaName.Equals(ISchemaDefinition.DefaultName, StringComparison.Ordinal);

    /// <summary>
    /// Gets the current request executor.
    /// </summary>
    public IRequestExecutor? CurrentExecutor { get; private set; }

    ulong IRequestExecutor.Version
        => CurrentExecutor?.Version ?? 0;

    ISchemaDefinition IRequestExecutor.Schema
        => CurrentExecutor?.Schema ?? throw new InvalidOperationException("No schema available yet.");

    IFeatureCollection IFeatureProvider.Features
        => CurrentExecutor?.Features ?? throw new InvalidOperationException("No feature collection available yet.");

    /// <summary>
    /// Executes the given GraphQL <paramref name="request" />.
    /// </summary>
    /// <param name="request">
    /// The GraphQL request object.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the execution result of the given GraphQL <paramref name="request" />.
    ///
    /// If the request operation is a simple query or mutation the result is a
    /// <see cref="OperationResult" />.
    ///
    /// If the request operation is a query or mutation where data is deferred, streamed or
    /// includes live data the result is a <see cref="IResponseStream" /> where each result
    /// that the <see cref="IResponseStream" /> yields is a <see cref="OperationResult" />.
    ///
    /// If the request operation is a subscription the result is a
    /// <see cref="IResponseStream" /> where each result that the
    /// <see cref="IResponseStream" /> yields is a
    /// <see cref="OperationResult" />.
    /// </returns>
    public async Task<IExecutionResult> ExecuteAsync(
        IOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var executor = CurrentExecutor ?? await GetExecutorAsync(cancellationToken).ConfigureAwait(false);
        return await executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the given GraphQL <paramref name="requestBatch" />.
    /// </summary>
    /// <param name="requestBatch">
    /// The GraphQL request batch.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a stream of query results.
    /// </returns>
    public async Task<IResponseStream> ExecuteBatchAsync(
        OperationRequestBatch requestBatch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestBatch);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var executor = CurrentExecutor ?? await GetExecutorAsync(cancellationToken).ConfigureAwait(false);
        return await executor.ExecuteBatchAsync(requestBatch, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves the schema for the specified schema name.
    /// </summary>
    /// <param name="cancellationToken">
    /// The request cancellation token.
    /// </param>
    /// <returns>
    /// Returns the resolved schema.
    /// </returns>
    public async ValueTask<ISchemaDefinition> GetSchemaAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var executor =
            await GetExecutorAsync(cancellationToken)
                .ConfigureAwait(false);
        return executor.Schema;
    }

    /// <summary>
    /// Resolves the executor for the specified schema name.
    /// </summary>
    /// <param name="cancellationToken">
    /// The request cancellation token.
    /// </param>
    /// <returns>
    /// Returns the resolved schema.
    /// </returns>
    public async ValueTask<IRequestExecutor> GetExecutorAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var executor = CurrentExecutor;

        if (executor is not null)
        {
            return executor;
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (CurrentExecutor is null)
            {
                executor = await _executorProvider
                    .GetExecutorAsync(_schemaName, cancellationToken)
                    .ConfigureAwait(false);

                OnConfigureRequestExecutor(executor, null);
                CurrentExecutor = executor;
            }
            else
            {
                executor = CurrentExecutor;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return executor;
    }

    protected virtual void OnConfigureRequestExecutor(IRequestExecutor newExecutor, IRequestExecutor? oldExecutor)
    {
    }

    protected virtual void OnAfterRequestExecutorSwapped(IRequestExecutor newExecutor, IRequestExecutor oldExecutor)
    {
    }

    private void OnRequestExecutorEvent(RequestExecutorEvent eventArgs)
    {
        if (_disposed || !eventArgs.Name.Equals(_schemaName) || CurrentExecutor is null)
        {
            return;
        }

        if (eventArgs.Type is RequestExecutorEventType.Created)
        {
            _events.Writer.TryWrite(eventArgs);
        }
    }

    private async Task OnUpdateRequestExecutorAsync(CancellationToken ct)
    {
        await foreach (var eventArgs in _events.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            IRequestExecutor newExecutor;
            IRequestExecutor oldExecutor;

            await _semaphore.WaitAsync(ct);

            try
            {
                // events are only raised when there is an initial executor
                // so its guaranteed that we have a CurrentExecutor at this point.
                oldExecutor = CurrentExecutor!;
                newExecutor = eventArgs.Executor;

                OnConfigureRequestExecutor(newExecutor, oldExecutor);

                // we only assign the executor after we have run configuration logic
                // on the new instance.
                CurrentExecutor = newExecutor;
            }
            finally
            {
                _semaphore.Release();
            }

            // after the swap of the executors we allow classes extending this proxy
            // to run notification logic that does not run within the lock context.
            OnAfterRequestExecutorSwapped(newExecutor, oldExecutor);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            CurrentExecutor = null;
            _cts.Cancel();
            _cts.Dispose();
            _events.Writer.TryComplete();
            _eventSubscription?.Dispose();
            _semaphore.Dispose();

            while (_events.Reader.TryRead(out _))
            {
                // we drain the channel
            }

            _disposed = true;
        }
    }
}
