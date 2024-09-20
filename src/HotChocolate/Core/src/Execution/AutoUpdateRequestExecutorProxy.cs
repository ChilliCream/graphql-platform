namespace HotChocolate.Execution;

/// <summary>
/// The <see cref="AutoUpdateRequestExecutorProxy"/> is a helper class that represents a
/// executor for one specific schema and handles the resolving and hot-swapping
/// the specific executor.
/// </summary>
public class AutoUpdateRequestExecutorProxy : IRequestExecutor, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly RequestExecutorProxy _executorProxy;
    private IRequestExecutor _executor;
    private bool _disposed;

    private AutoUpdateRequestExecutorProxy(
        RequestExecutorProxy requestExecutorProxy,
        IRequestExecutor initialExecutor)
    {
        _executorProxy = requestExecutorProxy;
        _executor = initialExecutor;

        _executorProxy.ExecutorEvicted += (_, _) => BeginUpdateExecutor();

        BeginUpdateExecutor();
    }

    /// <summary>
    /// The inner executor is exposed for testability.
    /// </summary>
    internal IRequestExecutor InnerExecutor => _executor;

    /// <summary>
    /// Creates a new auto-update proxy for <see cref="IRequestExecutor"/>.
    /// </summary>
    /// <param name="requestExecutorProxy">
    /// The underlying manual proxy.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a new auto-update proxy for <see cref="IRequestExecutor"/>.
    /// </returns>
    public static async ValueTask<AutoUpdateRequestExecutorProxy> CreateAsync(
        RequestExecutorProxy requestExecutorProxy,
        CancellationToken cancellationToken = default)
    {
        if (requestExecutorProxy == null)
        {
            throw new ArgumentNullException(nameof(requestExecutorProxy));
        }

        var executor = await requestExecutorProxy
            .GetRequestExecutorAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AutoUpdateRequestExecutorProxy(requestExecutorProxy, executor);
    }

    /// <summary>
    /// Creates a new auto-update proxy for <see cref="IRequestExecutor"/>.
    /// </summary>
    /// <param name="requestExecutorProxy">
    /// The underlying manual proxy.
    /// </param>
    /// <param name="initialExecutor">
    /// The initial executor instance.
    /// </param>
    /// <returns>
    /// Returns a new auto-update proxy for <see cref="IRequestExecutor"/>.
    /// </returns>
    public static AutoUpdateRequestExecutorProxy Create(
        RequestExecutorProxy requestExecutorProxy,
        IRequestExecutor initialExecutor)
    {
        if (requestExecutorProxy == null)
        {
            throw new ArgumentNullException(nameof(requestExecutorProxy));
        }

        return new AutoUpdateRequestExecutorProxy(requestExecutorProxy, initialExecutor);
    }

    /// <summary>
    /// Gets the schema to which this executor is bound to.
    /// </summary>
    public ISchema Schema => _executor.Schema;

    /// <summary>
    /// Gets the services that are bound to this executor.
    /// </summary>
    public IServiceProvider Services => _executor.Services;

    /// <summary>
    /// Gets the version of the executor.
    /// </summary>
    public ulong Version => _executor.Version;

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
    /// <see cref="IOperationResult" />.
    ///
    /// If the request operation is a query or mutation where data is deferred, streamed or
    /// includes live data the result is a <see cref="IResponseStream" /> where each result
    /// that the <see cref="IResponseStream" /> yields is a <see cref="IOperationResult" />.
    ///
    /// If the request operation is a subscription the result is a
    /// <see cref="IResponseStream" /> where each result that the
    /// <see cref="IResponseStream" /> yields is a
    /// <see cref="IOperationResult" />.
    /// </returns>
    public Task<IExecutionResult> ExecuteAsync(
        IOperationRequest request,
        CancellationToken cancellationToken = default)
        => _executor.ExecuteAsync(request, cancellationToken);

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
    public Task<IResponseStream> ExecuteBatchAsync(
        OperationRequestBatch requestBatch,
        CancellationToken cancellationToken = default)
        => _executor.ExecuteBatchAsync(requestBatch, cancellationToken);

    private void BeginUpdateExecutor()
        => Task.Run(UpdateExecutorAsync);

    private async ValueTask UpdateExecutorAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            var executor = await _executorProxy
                .GetRequestExecutorAsync(default)
                .ConfigureAwait(false);
            _executor = executor;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc cref="IDisposable" />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _executorProxy.Dispose();
            _semaphore.Dispose();
            _executor = null!;
            _disposed = true;
        }
    }
}
