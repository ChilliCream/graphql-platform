using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Utilities;
using static HotChocolate.Execution.Utilities.ResolverExecutionHelper;

namespace HotChocolate.Execution
{
    internal sealed class QueryExecutor : IOperationExecutor
    {
        public async Task<IExecutionResult> ExecuteAsync(
            IOperationContext operationContext,
            CancellationToken cancellationToken)
        {
            var scopedContext = ImmutableDictionary<string, object?>.Empty;

            IPreparedSelectionList rootSelections =
                operationContext.Operation.GetRootSelections();

            ResultMap resultMap = rootSelections.EnqueueResolverTasks(
                operationContext, n => Path.New(n), scopedContext,
                operationContext.RootValue);

            int proposedTaskCount = operationContext.Operation.ProposedTaskCount;
            var tasks = new Task[proposedTaskCount];

            for (int i = 0; i < proposedTaskCount; i++)
            {
                tasks[i] = StartExecutionTaskAsync(
                    operationContext.Execution,
                    operationContext.Execution.Completed);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            operationContext.Result.SetData(resultMap);
            return operationContext.Result.BuildResult();
        }
    }
}
