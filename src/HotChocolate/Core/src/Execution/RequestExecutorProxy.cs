using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution;

/// <summary>
/// The <see cref="RequestExecutorProxy"/> is a helper class that represents a executor for
/// one specific schema and handles the resolving and hot-swapping the specific executor.
/// </summary>
public sealed class RequestExecutorProxy : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IRequestExecutorResolver _executorResolver;
    private readonly string _schemaName;
    private IRequestExecutor? _executor;
    private readonly IDisposable? _eventSubscription;
    private bool _disposed;

    public event EventHandler<RequestExecutorUpdatedEventArgs>? ExecutorUpdated;

    public event EventHandler? ExecutorEvicted;

    public RequestExecutorProxy(IRequestExecutorResolver executorResolver, string schemaName)
    {
        if (string.IsNullOrEmpty(schemaName))
        {
            throw new ArgumentNullException(nameof(schemaName));
        }

        _executorResolver = executorResolver ??
            throw new ArgumentNullException(nameof(executorResolver));
        _schemaName = schemaName;
        _eventSubscription =
            _executorResolver.Events.Subscribe(
                new ExecutorObserver(EvictRequestExecutor));
    }

    public IRequestExecutor? CurrentExecutor => _executor;

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
    public async Task<IExecutionResult> ExecuteAsync(
        IOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var executor =
            await GetRequestExecutorAsync(cancellationToken)
                .ConfigureAwait(false);

        var result =
            await executor
                .ExecuteAsync(request, cancellationToken)
                .ConfigureAwait(false);

        return result;
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
        if (requestBatch == null)
        {
            throw new ArgumentNullException(nameof(requestBatch));
        }

        var executor =
            await GetRequestExecutorAsync(cancellationToken)
                .ConfigureAwait(false);

        var result =
            await executor
                .ExecuteBatchAsync(requestBatch, cancellationToken)
                .ConfigureAwait(false);

        return result;
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
    public async ValueTask<ISchema> GetSchemaAsync(
        CancellationToken cancellationToken)
    {
        var executor =
            await GetRequestExecutorAsync(cancellationToken)
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
    public async ValueTask<IRequestExecutor> GetRequestExecutorAsync(
        CancellationToken cancellationToken)
    {
        var executor = _executor;

        if (executor is not null)
        {
            return executor;
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_executor is null)
            {
                executor = await _executorResolver
                    .GetRequestExecutorAsync(_schemaName, cancellationToken)
                    .ConfigureAwait(false);

                _executor = executor;

                ExecutorUpdated?.Invoke(
                    this,
                    new RequestExecutorUpdatedEventArgs(executor));
            }
            else
            {
                executor = _executor;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return executor;
    }

    private void EvictRequestExecutor(string schemaName)
    {
        if (!_disposed && schemaName.Equals(_schemaName))
        {
            _semaphore.Wait();

            try
            {
                _executor = null;
                ExecutorEvicted?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _executor = null;
            _eventSubscription?.Dispose();
            _semaphore.Dispose();
            _disposed = true;
        }
    }

    private sealed class ExecutorObserver(Action<string> evicted) : IObserver<RequestExecutorEvent>
    {
        public void OnNext(RequestExecutorEvent value)
        {
            if (value.Type is RequestExecutorEventType.Evicted)
            {
                evicted(value.Name);
            }
        }

        public void OnError(Exception error) { }

        public void OnCompleted() { }
    }
}
