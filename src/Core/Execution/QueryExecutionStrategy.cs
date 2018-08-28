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
            var data = new OrderedDictionary();

            IEnumerable<ResolverTask> rootResolverTasks =
                CreateRootResolverTasks(executionContext, data);

            await ExecuteResolversAsync(
                executionContext, rootResolverTasks,
                cancellationToken);

            return new QueryResult(data, executionContext.GetErrors());
        }
    }
}
