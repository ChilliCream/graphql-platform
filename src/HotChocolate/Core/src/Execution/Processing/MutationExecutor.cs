using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;

namespace HotChocolate.Execution.Processing
{
    internal sealed class MutationExecutor
    {
        public Task<IQueryResult> ExecuteAsync(
            IOperationContext operationContext)
        {
            if (operationContext is null)
            {
                throw new ArgumentNullException(nameof(operationContext));
            }

            ISelectionSet selectionSet = operationContext.Operation.GetRootSelectionSet();
            IReadOnlyList<ISelection> selections = selectionSet.Selections;
            ResultMap resultMap = operationContext.Result.RentResultMap(selections.Count);

            return ExecuteSelectionsAsync(operationContext, selections, resultMap);
        }

        private static async Task<IQueryResult> ExecuteSelectionsAsync(
            IOperationContext operationContext,
            IReadOnlyList<ISelection> selections,
            ResultMap resultMap)
        {
            var responseIndex = 0;
            ImmutableDictionary<string, object?> scopedContext =
                ImmutableDictionary<string, object?>.Empty;

            for (var i = 0; i < selections.Count; i++)
            {
                ISelection selection = selections[i];
                if (selection.IsIncluded(operationContext.Variables))
                {
                    IExecutionTask task = CreateResolverTask(
                        operationContext,
                        selection,
                        operationContext.RootValue,
                        responseIndex++,
                        Path.New(selection.ResponseName),
                        resultMap,
                        scopedContext);

                    operationContext.Execution.Work.Register(task);

                    await ExecutionTaskProcessor.ExecuteAsync(operationContext).ConfigureAwait(false);

                    if (i + 1 < selections.Count)
                    {
                        operationContext.Execution.Reset();
                    }
                }
            }

            return operationContext
                .TrySetNext()
                .SetData(resultMap)
                .BuildResult();
        }
    }
}
