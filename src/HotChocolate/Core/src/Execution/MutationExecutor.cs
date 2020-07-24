using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using HotChocolate.Execution.Utilities;
using static HotChocolate.Execution.Utilities.ResolverExecutionHelper;

namespace HotChocolate.Execution
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
            var scopedContext = ImmutableDictionary<string, object?>.Empty;
            IPreparedSelectionList rootSelections = operationContext.Operation.GetRootSelections();
            ResultMap resultMap = operationContext.Result.RentResultMap(rootSelections.Count);

            for (var i = 0; i < rootSelections.Count; i++)
            {
                IPreparedSelection selection = rootSelections[i];
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

            operationContext.Result.SetData(resultMap);
            return operationContext.Result.BuildResult();
        }
    }
}
