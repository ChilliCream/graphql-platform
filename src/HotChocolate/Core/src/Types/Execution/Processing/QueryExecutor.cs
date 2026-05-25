using System.Collections.Immutable;
using System.Diagnostics;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;

namespace HotChocolate.Execution.Processing;

internal sealed class QueryExecutor
{
    public Task<IExecutionResult> ExecuteAsync(
        OperationContext operationContext)
        => ExecuteAsync(operationContext, ImmutableDictionary<string, object?>.Empty);

    public Task<IExecutionResult> ExecuteAsync(
        OperationContext operationContext,
        IImmutableDictionary<string, object?> scopedContext)
    {
        if (operationContext.Operation.HasIncrementalParts)
        {
            return ExecuteIncrementalAsync(operationContext, scopedContext);
        }

        return ExecuteNoIncrementalAsync(operationContext, scopedContext);
    }

    private static async Task<IExecutionResult> ExecuteIncrementalAsync(
        OperationContext operationContext,
        IImmutableDictionary<string, object?> scopedContext)
    {
        EnqueueRootResolverTasks(
            operationContext,
            operationContext.RootValue,
            operationContext.Result.Data.Data,
            scopedContext);

        var branchId = operationContext.ExecutionBranchId;
        var scheduler = operationContext.Scheduler;
        var coordinator = operationContext.DeferExecutionCoordinator;

        var execution = scheduler.ExecuteAsync1();
        await scheduler.WaitForCompletionAsync(branchId).ConfigureAwait(false);
        var initialResult = operationContext.BuildResult();

        if (!coordinator.HasBranches)
        {
            if (!execution.IsCompletedSuccessfully)
            {
                await execution.ConfigureAwait(false);
            }

            return initialResult;
        }

        coordinator.EnqueueResult(initialResult);
        return new ResponseStream(CreateStream, ExecutionResultKind.DeferredResult);

        async IAsyncEnumerable<OperationResult> CreateStream()
        {
            var requestAborted = operationContext.RequestAborted;
            await foreach (var result in coordinator.ReadResultsAsync(requestAborted))
            {
                yield return result;
            }

            await execution.ConfigureAwait(false);
        }
    }

    private static async Task<IExecutionResult> ExecuteNoIncrementalAsync(
        OperationContext operationContext,
        IImmutableDictionary<string, object?> scopedContext)
    {
        EnqueueRootResolverTasks(
            operationContext,
            operationContext.RootValue,
            operationContext.Result.Data.Data,
            scopedContext);

        await operationContext.Scheduler.ExecuteAsync1().ConfigureAwait(false);

        return operationContext.BuildResult();
    }

    public Task ExecuteBatchAsync(
        OperationContextOwner[] operationContexts,
        IExecutionResult[] results,
        int length)
    {
        Debug.Assert(length > 0);
        Debug.Assert(length <= operationContexts.Length);
        Debug.Assert(length <= results.Length);

        if (operationContexts[0].OperationContext.Operation.HasIncrementalParts)
        {
            return ExecuteBatchIncrementalAsync(operationContexts, results, length);
        }

        return ExecuteBatchNoIncrementalAsync(operationContexts, results, length);
    }

    private async Task ExecuteBatchNoIncrementalAsync(
        OperationContextOwner[] operationContexts,
        IExecutionResult[] results,
        int length)
    {
        // when using batching we will use the same scheduler
        // to execute more efficiently with DataLoader.
        var parentContext = operationContexts[0].OperationContext;

        FillSchedulerWithWork(parentContext, operationContexts, length);

        await parentContext.Scheduler.ExecuteAsync1().ConfigureAwait(false);

        for (var i = 0; i < length; ++i)
        {
            results[i] = operationContexts[i].OperationContext.BuildResult();
        }
    }

    private async Task ExecuteBatchIncrementalAsync(
        OperationContextOwner[] operationContexts,
        IExecutionResult[] results,
        int length)
    {
        // when using batching we will use the same scheduler
        // to execute more efficiently with DataLoader.
        var parentContext = operationContexts[0].OperationContext;
        var scheduler = parentContext.Scheduler;

        FillSchedulerWithWork(parentContext, operationContexts, length);

        var execution = parentContext.Scheduler.ExecuteAsync1();

        for (var i = 0; i < length; ++i)
        {
            if (i == 0)
            {
                var branchId = parentContext.ExecutionBranchId;
                await scheduler.WaitForCompletionAsync(branchId).ConfigureAwait(false);
                parentContext.DeferExecutionCoordinator.EnqueueResult(parentContext.BuildResult());
                results[i] = new ResponseStream(CreateStreamAndComplete, ExecutionResultKind.DeferredResult);
            }
            else
            {
                var context = operationContexts[i].OperationContext;
                var branchId = context.ExecutionBranchId;
                await scheduler.WaitForCompletionAsync(branchId).ConfigureAwait(false);
                context.DeferExecutionCoordinator.EnqueueResult(context.BuildResult());
                results[i] = new ResponseStream(CreateStream, ExecutionResultKind.DeferredResult);
            }
        }

        async IAsyncEnumerable<OperationResult> CreateStreamAndComplete()
        {
            var requestAborted = parentContext.RequestAborted;
            await foreach (var result in parentContext.DeferExecutionCoordinator.ReadResultsAsync(requestAborted))
            {
                yield return result;
            }

            await execution.ConfigureAwait(false);
        }

        IAsyncEnumerable<OperationResult> CreateStream()
        {
            var requestAborted = parentContext.RequestAborted;
            return parentContext.DeferExecutionCoordinator.ReadResultsAsync(requestAborted);
        }
    }

    private static void FillSchedulerWithWork(
        OperationContext parentContext,
        OperationContextOwner[] operationContexts,
        int length)
    {
        for (var i = 0; i < length; i++)
        {
            var context = operationContexts[i].OperationContext;
            context.InitializeWorkSchedulerFrom(parentContext);

            EnqueueRootResolverTasks(
                context,
                context.RootValue,
                context.Result.Data.Data,
                ImmutableDictionary<string, object?>.Empty);
        }
    }
}
