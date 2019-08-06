using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Batching
{
    public class BatchQueryExecutor
        : IBatchQueryExecutor
    {
        private readonly IQueryExecutor _executor;
        private readonly IErrorHandler _errorHandler;

        public BatchQueryExecutor(
            IQueryExecutor executor,
            IErrorHandler errorHandler)
        {
            _executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            _errorHandler = errorHandler
                ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public ISchema Schema => _executor.Schema;

        public Task<IBatchQueryExecutionResult> ExecuteAsync(
            IReadOnlyList<IReadOnlyQueryRequest> batch,
            CancellationToken cancellationToken)
        {
            if (batch == null || batch.Count == 0)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            return Task.FromResult<IBatchQueryExecutionResult>(
                new BatchQueryExecutionResult(
                    _executor,
                    _errorHandler,
                    batch[0].Services.GetTypeConversion(),
                    batch));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // we do not have anything to dispose.
        }
    }
}
