using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal sealed class QueryExecutionStrategy
        : ExecutionStrategyBase
    {
        public override Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            return ExecuteInternalAsync(executionContext, cancellationToken);
        }

        private static async Task<IExecutionResult> ExecuteInternalAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            BatchOperationHandler batchOperationHandler =
                CreateBatchOperationHandler(executionContext);

            try
            {
                return await ExecuteQueryAsync(
                    executionContext,
                    batchOperationHandler,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                batchOperationHandler?.Dispose();
            }
        }
    }
}
