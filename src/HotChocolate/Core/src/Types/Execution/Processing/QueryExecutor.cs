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

        // capture the arena now: the request executor detaches it from the context once this
        // method returns the stream, so the deferred closure below seals the captured arena.
        var memory = operationContext.Memory;

        var execution = scheduler.ExecuteAsync1();
        await scheduler.WaitForCompletionAsync(branchId).ConfigureAwait(false);
        var initialResult = operationContext.BuildResult();

        if (!coordinator.HasBranches)
        {
            if (!execution.IsCompletedSuccessfully)
            {
                await execution.ConfigureAwait(false);
            }

            // there are no deferred parts, so this behaves like a buffered result: seal now.
            memory.Seal();
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

            // The stream was read to completion, so every deferred branch has been delivered and
            // the scheduler has fully settled: no resolver or batch task can write into the arena
            // anymore, so we seal it to allow its memory to be returned on dispose.
            // On cancellation or early disposal we never reach here and the arena is abandoned,
            // because in-flight parallel work (including the batch dispatcher) may still write to it.
            await execution.ConfigureAwait(false);
            memory.Seal();
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

        var result = operationContext.BuildResult();

        // the result is complete and nothing else writes into the request memory,
        // so we seal it to allow its pages to be returned to the pool on dispose.
        operationContext.Memory.Seal();

        return result;
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

        // all results in the batch share one request arena; every result has been built and
        // nothing else writes into it, so we seal it once for the whole batch.
        parentContext.Memory.Seal();
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

        // capture the shared arena now: the request executor detaches it once we return the
        // streams, so the completing closure below seals the captured arena.
        var memory = parentContext.Memory;

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

            // The batch streams were read to completion, so the scheduler has fully settled and
            // nothing can write into the shared arena anymore, so we seal it for reuse.
            // On cancellation or early disposal we never reach here and the arena is abandoned,
            // because in-flight parallel work (including the batch dispatcher) may still write to it.
            await execution.ConfigureAwait(false);
            memory.Seal();
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
