using System;
using System.Collections.Immutable;

namespace HotChocolate.Execution.Utilities
{
    internal static class PreparedSelectionListExtensions
    {
        public static ResultMap EnqueueResolverTasks(
            this IPreparedSelectionList selections,
            IOperationContext operationContext,
            Func<NameString, Path> createPath,
            IImmutableDictionary<string, object?> scopedContext,
            object? parent)
        {
            var responseIndex = 0;
            ResultMap resultMap = operationContext.Result.RentResultMap(selections.Count);

            for (var i = 0; i < selections.Count; i++)
            {
                IPreparedSelection selection = selections[i];
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
