using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static HotChocolate.Execution.Processing.ResolverExecutionHelper;

namespace HotChocolate.Execution.Processing
{
    internal sealed class MutationExecutor
    {
        public async Task<IReadOnlyQueryResult> ExecuteAsync(
            IOperationContext operationContext)
        {
            if (operationContext is null)
            {
                throw new ArgumentNullException(nameof(operationContext));
            }

            var responseIndex = 0;
            ImmutableDictionary<string, object?> scopedContext =
                ImmutableDictionary<string, object?>.Empty;
            ISelectionSet selectionSet = operationContext.Operation.GetRootSelectionSet();
            IReadOnlyList<ISelection> selections = selectionSet.Selections;
            ResultMap resultMap = operationContext.Result.RentResultMap(selections.Count);

            for (var i = 0; i < selections.Count; i++)
            {
                ISelection selection = selectionSet.Selections[i];
                if (selection.IsIncluded(operationContext.Variables))
                {
                    operationContext.Execution.TaskBacklog.Register(
                        new ResolverTaskDefinition(
                            operationContext,
                            selection,
                            responseIndex++,
                            resultMap,
                            operationContext.RootValue,
                            Path.New(selection.ResponseName),
                            scopedContext));

                    await ExecuteTasksAsync(operationContext);
                }
            }

            operationContext.TrySetNext();
            operationContext.Result.SetData(resultMap);
            return operationContext.Result.BuildResult();
        }
    }
}
