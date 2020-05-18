using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class QueryExecutor : IOperationExecutor
    {
        public async Task<IExecutionResult> ExecuteAsync(
            IOperationContext operationContext,
            CancellationToken cancellationToken)
        {
            var scopedContext = ImmutableDictionary<string, object?>.Empty;
            IPreparedSelectionList rootSelections = operationContext.Operation.GetRootSelections();
            ResultMap resultMap = rootSelections.EnqueueResolverTasks(
                operationContext, n => Path.New(n), scopedContext);

            await ResolverExecutionHelper.ExecuteResolversAsync(
                operationContext.Execution,
                cancellationToken)
                .ConfigureAwait(false);

            operationContext.Result.SetData(resultMap);
            return operationContext.Result.BuildResult();
        }
    }
}
