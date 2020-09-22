using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Execution.Utilities
{
    internal static class SelectionSetExtensions
    {
        public static ResultMap EnqueueResolverTasks(
            this ISelectionSet selectionSet,
            IOperationContext operationContext,
            Func<NameString, Path> createPath,
            IImmutableDictionary<string, object?> scopedContext,
            object? parent)
        {
            var responseIndex = 0;
            IReadOnlyList<ISelection> selections = selectionSet.Selections;
            ResultMap resultMap = operationContext.Result.RentResultMap(selections.Count);

            for (var i = 0; i < selections.Count; i++)
            {
                ISelection selection = selections[i];
                if (selection.IsIncluded(operationContext.Variables))
                {
                    operationContext.Execution.TaskBacklog.Register(
                        new ResolverTaskDefinition(
                            operationContext,
                            selection,
                            responseIndex++,
                            resultMap,
                            parent,
                            createPath(selection.ResponseName),
                            scopedContext));
                }
            }

            return resultMap;
        }
    }
}
