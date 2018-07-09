using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal class QueryExecutionStrategy
        : ExecutionStrategyBase
    {
        public override async Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

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
