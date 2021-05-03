using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing
{
    internal static class SelectionSetExtensions
    {
        public static ResultMap EnqueueResolverTasks(
            this ISelectionSet selectionSet,
            IOperationContext operationContext,
            Path path,
            IImmutableDictionary<string, object?> scopedContext,
            object? parent)
        {
            var responseIndex = 0;
            IReadOnlyList<ISelection> selections = selectionSet.Selections;
            ResultMap resultMap = operationContext.Result.RentResultMap(selections.Count);

            BatchExecutionTask? batchExecutionTask = null;
            IExecutionTask? single = null;

            for (var i = 0; i < selections.Count; i++)
            {
                ISelection selection = selections[i];
                if (selection.IsIncluded(operationContext.Variables))
                {
                    IExecutionTask task = operationContext.Execution.CreateTask(
                        new ResolverTaskDefinition(
                            operationContext,
                            selection,
                            responseIndex++,
                            resultMap,
                            parent,
                            path.Append(selection.ResponseName),
                            scopedContext));

                    if (batchExecutionTask is not null)
                    {
                        batchExecutionTask.AddExecutionTask(task);
                    }
                    else if (single is null)
                    {
                        single = task;
                    }
                    else
                    {
                        batchExecutionTask = operationContext.Execution.CreateBatchTask(operationContext);
                        batchExecutionTask.AddExecutionTask(single);
                        batchExecutionTask.AddExecutionTask(task);
                        single = null;
                    }
                }
            }

            if (single is not null || batchExecutionTask is not null)
            {
                operationContext.Execution.TaskBacklog.Register(single ?? batchExecutionTask!);
            }

            if (selectionSet.Fragments.Count > 0)
            {
                IReadOnlyList<IFragment> fragments = selectionSet.Fragments;
                for (var i = 0; i < fragments.Count; i++)
                {
                    IFragment fragment = fragments[i];
                    if (!fragment.IsConditional)
                    {
                        operationContext.Execution.DeferredTaskBacklog.Register(
                            new DeferredFragment(
                                fragment,
                                fragment.GetLabel(operationContext.Variables),
                                path,
                                parent,
                                scopedContext));
                    }
                }
            }

            return resultMap;
        }
    }
}
