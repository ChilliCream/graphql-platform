using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;

namespace HotChocolate.Execution.Processing;

internal sealed class QueryExecutor
{
    public Task<IQueryResult> ExecuteAsync(
        OperationContext operationContext) =>
        ExecuteAsync(operationContext, ImmutableDictionary<string, object?>.Empty);

    public Task<IQueryResult> ExecuteAsync(
        OperationContext operationContext,
        IImmutableDictionary<string, object?> scopedContext)
    {
        if (operationContext is null)
        {
            throw new ArgumentNullException(nameof(operationContext));
        }

        if (scopedContext is null)
        {
            throw new ArgumentNullException(nameof(scopedContext));
        }

        return ExecuteInternalAsync(operationContext, scopedContext);
    }

    private static async Task<IQueryResult> ExecuteInternalAsync(
        OperationContext operationContext,
        IImmutableDictionary<string, object?> scopedContext)
    {
        var resultMap = EnqueueResolverTasks(
            operationContext,
            operationContext.Operation.RootSelectionSet,
            operationContext.RootValue,
            Path.Root,
            scopedContext);

        await operationContext.Scheduler.ExecuteAsync().ConfigureAwait(false);

        return operationContext.SetData(resultMap).BuildResult();
    }
}
