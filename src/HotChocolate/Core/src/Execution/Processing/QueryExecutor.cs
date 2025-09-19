using System.Collections.Immutable;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;

namespace HotChocolate.Execution.Processing;

internal sealed class QueryExecutor
{
    public Task<IOperationResult> ExecuteAsync(
        OperationContext operationContext)
        => ExecuteAsync(operationContext, ImmutableDictionary<string, object?>.Empty);

    public Task<IOperationResult> ExecuteAsync(
        OperationContext operationContext,
        IImmutableDictionary<string, object?> scopedContext)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(scopedContext);

        return ExecuteInternalAsync(operationContext, scopedContext);
    }

    private static async Task<IOperationResult> ExecuteInternalAsync(
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

    public async Task ExecuteBatchAsync(
        ReadOnlyMemory<OperationContextOwner> operationContexts,
        Memory<IOperationResult> results)
    {
        var scopedContext = ImmutableDictionary<string, object?>.Empty;

        // when using batching we will use the same scheduler
        // to execute more efficiently with DataLoader.
        var scheduler = operationContexts.Span[0].OperationContext.Scheduler;

        FillSchedulerWithWork(scheduler, operationContexts.Span, scopedContext);

        await scheduler.ExecuteAsync().ConfigureAwait(false);

        BuildResults(operationContexts.Span, results.Span);
    }

    private static void FillSchedulerWithWork(
        WorkScheduler scheduler,
        ReadOnlySpan<OperationContextOwner> operationContexts,
        ImmutableDictionary<string, object?> scopedContext)
    {
        foreach (var contextOwner in operationContexts)
        {
            var context = contextOwner.OperationContext;
            context.Scheduler = scheduler;

            var resultMap = EnqueueResolverTasks(
                context,
                context.Operation.RootSelectionSet,
                context.RootValue,
                Path.Root,
                scopedContext);

            context.SetData(resultMap);
        }
    }

    private static void BuildResults(
        ReadOnlySpan<OperationContextOwner> operationContexts,
        Span<IOperationResult> results)
    {
        for (var i = 0; i < operationContexts.Length; ++i)
        {
            results[i] = operationContexts[i].OperationContext.BuildResult();
        }
    }
}
