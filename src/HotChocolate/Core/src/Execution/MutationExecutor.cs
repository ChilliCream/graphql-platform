using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class MutationExecutor
        : IOperationExecutor
    {
        public async Task<IExecutionResult> ExecuteAsync(
            IOperationContext operationContext,
            CancellationToken cancellationToken)
        {
            int responseIndex = 0;
            var scopedContext = ImmutableDictionary<string, object?>.Empty;
            IPreparedSelectionList rootSelections = operationContext.Operation.GetRootSelections();
            ResultMap resultMap = operationContext.Result.RentResultMap(rootSelections.Count);

            for (int i = 0; i < rootSelections.Count; i++)
            {
                IPreparedSelection selection = rootSelections[i];
                if (selection.IsVisible(operationContext.Variables))
                {
                    operationContext.Execution.Tasks.Enqueue(
                        selection,
                        responseIndex++,
                        resultMap,
                        operationContext.RootValue,
                        Path.New(selection.ResponseName),
                        scopedContext);

                    await ResolverExecutionHelper.ExecuteResolversAsync(
                        operationContext.Execution,
                        cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            operationContext.Result.SetData(resultMap);
            return operationContext.Result.BuildResult();
        }        
    }
}
