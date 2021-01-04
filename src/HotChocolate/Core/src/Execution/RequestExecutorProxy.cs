using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    /// <summary>
    /// The <see cref="RequestExecutorProxy"/> is a helper class that represents a executor for
    /// one specific schema and handles the resolving and hot-swapping the specific executor.
    /// </summary>
    public class RequestExecutorProxy : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IRequestExecutorResolver _executorResolver;
        private readonly NameString _schemaName;
        private IRequestExecutor? _executor;
        private bool _disposed;

        public event EventHandler<RequestExecutorUpdatedEventArgs>? ExecutorUpdated;

        public event EventHandler? ExecutorEvicted;

        public RequestExecutorProxy(
            IRequestExecutorResolver executorResolver,
            NameString schemaName)
        {
            _executorResolver = executorResolver ??
                throw new ArgumentNullException(nameof(executorResolver));
            _schemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
            _executorResolver.RequestExecutorEvicted += EvictRequestExecutor;
        }

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
        /// <see cref="IQueryResult" />.
        ///
        /// If the request operation is a query or mutation where data is deferred, streamed or
        /// includes live data the result is a <see cref="IResponseStream" /> where each result
        /// that the <see cref="IResponseStream" /> yields is a <see cref="IQueryResult" />.
        ///
        /// If the request operation is a subscription the result is a
        /// <see cref="IResponseStream" /> where each result that the
        /// <see cref="IResponseStream" /> yields is a
        /// <see cref="IQueryResult" />.
        /// </returns>
        public async Task<IExecutionResult> ExecuteAsync(
            IQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IRequestExecutor executor =
                await GetRequestExecutorAsync(cancellationToken)
                    .ConfigureAwait(false);

            IExecutionResult result =
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
        /// <param name="allowParallelExecution">
        /// Defines if the executor is allowed to execute the batch in parallel.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// Returns a stream of query results.
        /// </returns>
        public async Task<IBatchQueryResult> ExecuteBatchAsync(
            IEnumerable<IQueryRequest> requestBatch,
            bool allowParallelExecution = false,
            CancellationToken cancellationToken = default)
        {
            if (requestBatch == null)
            {
                throw new ArgumentNullException(nameof(requestBatch));
            }

            IRequestExecutor executor =
                await GetRequestExecutorAsync(cancellationToken)
                    .ConfigureAwait(false);

            IBatchQueryResult result =
                await executor
                    .ExecuteBatchAsync(requestBatch, allowParallelExecution, cancellationToken)
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
            IRequestExecutor executor =
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
            IRequestExecutor? executor = _executor;

            if (executor is null)
            {
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
            }

            return executor;
        }

        private void EvictRequestExecutor(object? sender, RequestExecutorEvictedEventArgs args)
        {
            if (!_disposed && args.Name.Equals(_schemaName))
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _executor = null;
                _semaphore.Dispose();
                _disposed = true;
            }
        }
    }
}
