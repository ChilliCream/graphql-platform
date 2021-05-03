using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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

            BatchResolverTask? batchExecutionTask = null;
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

                    if (selection.PureResolver is not null)
                    {
                        batchExecutionTask ??= new BatchResolverTask(operationContext);
                        batchExecutionTask.Tasks.Add(task);
                    }
                    else
                    {
                        operationContext.Execution.TaskBacklog.Register(task);
                    }
                }
            }

            if (batchExecutionTask is not null)
            {
                operationContext.Execution.TaskBacklog.Register(batchExecutionTask);
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
