using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;

namespace HotChocolate.Execution.Processing
{
    internal sealed class MutationExecutor
    {
        public Task<IQueryResult> ExecuteAsync(IOperationContext operationContext)
        {
            if (operationContext is null)
            {
                throw new ArgumentNullException(nameof(operationContext));
            }

            if (operationContext is null)
            {
                throw new ArgumentNullException(nameof(operationContext));
            }

            return ExecuteInternalAsync(operationContext);
        }

        private static async Task<IQueryResult> ExecuteInternalAsync(
            IOperationContext operationContext)
        {
            ISelectionSet rootSelections =
                operationContext.Operation.GetRootSelectionSet();

            ResultMap resultMap = EnqueueResolverTasks(
                operationContext,
                rootSelections,
                operationContext.RootValue,
                Path.Root,
                ImmutableDictionary<string, object?>.Empty);

            await ExecutionTaskProcessor.ExecuteAsync(operationContext).ConfigureAwait(false);

            return operationContext
                .TrySetNext()
                .SetData(resultMap)
                .BuildResult();
        }
    }
}
