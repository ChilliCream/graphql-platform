using System.Collections.Immutable;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;

namespace HotChocolate.Execution.Processing;

internal sealed class QueryExecutor
{
    public Task<OperationResult> ExecuteAsync(
        OperationContext operationContext)
        => ExecuteAsync(operationContext, ImmutableDictionary<string, object?>.Empty);

    public Task<OperationResult> ExecuteAsync(
        OperationContext operationContext,
        IImmutableDictionary<string, object?> scopedContext)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(scopedContext);

        return ExecuteInternalAsync(operationContext, scopedContext);
    }

    private static async Task<OperationResult> ExecuteInternalAsync(
        OperationContext operationContext,
        IImmutableDictionary<string, object?> scopedContext)
    {
        EnqueueResolverTasks(
            operationContext,
            operationContext.RootValue,
            operationContext.Result.Data.Data,
            scopedContext,
            Path.Root);

        await operationContext.Scheduler.ExecuteAsync().ConfigureAwait(false);

        return operationContext.BuildResult();
    }

    public async Task ExecuteBatchAsync(
        ReadOnlyMemory<OperationContextOwner> operationContexts,
        Memory<IExecutionResult> results)
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

            EnqueueResolverTasks(
                context,
                context.RootValue,
                context.Result.Data.Data,
                scopedContext,
                Path.Root);
        }
    }

    private static void BuildResults(
        ReadOnlySpan<OperationContextOwner> operationContexts,
        Span<IExecutionResult> results)
    {
        for (var i = 0; i < operationContexts.Length; ++i)
        {
            results[i] = operationContexts[i].OperationContext.BuildResult();
        }
    }
}
