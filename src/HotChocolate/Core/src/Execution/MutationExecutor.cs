using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class MutationExecutor
    {
        public async Task<IReadOnlyQueryResult> ExecuteAsync(
            IOperationContext operationContext)
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
                    operationContext.Execution.TaskBacklog.Register(
                        new ResolverTaskDefinition(
                            operationContext,
                            selection,
                            responseIndex++,
                            resultMap,
                            operationContext.RootValue,
                            Path.New(selection.ResponseName),
                            scopedContext));

                    await ResolverExecutionHelper.StartExecutionTaskAsync(
                        operationContext.Execution,
                        operationContext.RequestAborted)
                        .ConfigureAwait(false);
                }
            }

            operationContext.Result.SetData(resultMap);
            return operationContext.Result.BuildResult();
        }
    }
}
