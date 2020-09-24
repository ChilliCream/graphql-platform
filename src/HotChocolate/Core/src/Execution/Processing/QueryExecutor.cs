using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static HotChocolate.Execution.Processing.ResolverExecutionHelper;

namespace HotChocolate.Execution.Processing
{
    internal sealed class QueryExecutor
    {
        public Task<IReadOnlyQueryResult> ExecuteAsync(
            IOperationContext operationContext) =>
            ExecuteAsync(operationContext, ImmutableDictionary<string, object?>.Empty);

        public async Task<IReadOnlyQueryResult> ExecuteAsync(
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

            ISelectionSet rootSelections =
                operationContext.Operation.GetRootSelectionSet();

            ResultMap resultMap = rootSelections.EnqueueResolverTasks(
                operationContext, Path.Root, scopedContext,
                operationContext.RootValue);

            await ExecuteTasksAsync(operationContext);

            operationContext.TrySetNext();
            operationContext.Result.SetData(resultMap);
            return operationContext.Result.BuildResult();
        }
    }
}
