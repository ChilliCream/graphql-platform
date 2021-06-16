using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;

namespace HotChocolate.Execution.Processing
{
    internal sealed class QueryExecutor
    {
        public Task<IQueryResult> ExecuteAsync(
            IOperationContext operationContext) =>
            ExecuteAsync(operationContext, ImmutableDictionary<string, object?>.Empty);

        public Task<IQueryResult> ExecuteAsync(
            IOperationContext operationContext,
            IImmutableDictionary<string, object?> scopedContext)
        {
            if (operationContext is null)
            {
                throw new ArgumentNullException(nameof(operationContext));
            }

            if (scopedContext is null)
            {
                throw new ArgumentNullException(nameof(scopedContext));
            }

            return ExecuteInternalAsync(operationContext, scopedContext);
        }

        private static async Task<IQueryResult> ExecuteInternalAsync(
            IOperationContext operationContext,
            IImmutableDictionary<string, object?> scopedContext)
        {
            ISelectionSet rootSelections =
                operationContext.Operation.GetRootSelectionSet();

            ResultMap resultMap = EnqueueResolverTasks(
                operationContext,
                rootSelections,
                operationContext.RootValue,
                Path.Root,
                scopedContext);

            await ExecutionTaskProcessor.ExecuteAsync(operationContext).ConfigureAwait(false);

            return operationContext
                .TrySetNext()
                .SetData(resultMap)
                .BuildResult();
        }
    }
}
