using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal class QueryExecutionStrategy
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

        private async Task<IExecutionResult> ExecuteInternalAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            return await ExecuteQueryAsync(executionContext, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
