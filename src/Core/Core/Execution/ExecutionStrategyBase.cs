using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using System.Runtime.CompilerServices;

namespace HotChocolate.Execution
{
    internal abstract partial class ExecutionStrategyBase
        : IExecutionStrategy
    {
        public abstract Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken);

        protected static async Task<IQueryResult> ExecuteQueryAsync(
            IExecutionContext executionContext,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            List<ResolverContext> initialBatch =
                CreateInitialBatch(
                    executionContext,
                    executionContext.Result.Data);
            try
            {
                await ExecuteResolversAsync(
                    executionContext,
                    initialBatch,
                    batchOperationHandler,
                    cancellationToken)
                    .ConfigureAwait(false);

                EnsureRootValueNonNullState(executionContext.Result, initialBatch);

                return executionContext.Result;
            }
            finally
            {
                ExecutionPools.ContextListPool.Return(initialBatch);
            }
        }

        protected static async Task ExecuteResolversAsync(
            IExecutionContext executionContext,
            IEnumerable<ResolverContext> initialBatch,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            List<ResolverContext> batch = ExecutionPools.ContextListPool.Get();
            List<ResolverContext> next = ExecutionPools.ContextListPool.Get();
            List<ResolverContext> swap = null;

            batch.AddRange(initialBatch);
            int batchSize = batch.Count;

            try
            {
                while (batchSize > 0)
                {
                    // start field resolvers
                    BeginExecuteResolverBatch(
                        batch,
                        executionContext.ErrorHandler,
                        cancellationToken);

                    // execute batch data loaders
                    if (batchOperationHandler != null)
                    {
                        await CompleteBatchOperationsAsync(
                            batch,
                            batchOperationHandler,
                            cancellationToken)
                            .ConfigureAwait(false);
                    }

                    // await field resolver results
                    await EndExecuteResolverBatchAsync(
                        batch,
                        next.Add,
                        cancellationToken)
                        .ConfigureAwait(false);

                    swap = batch;
                    batch = next;
                    next = swap;

                    next.Clear();
                    batchSize = batch.Count;

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                ExecutionPools.ContextListPool.Return(batch);
                ExecutionPools.ContextListPool.Return(next);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void EnsureRootValueNonNullState(
            IQueryResult result,
            IReadOnlyList<ResolverContext> initialBatch)
        {
            for (int i = 0; i < initialBatch.Count; i++)
            {
                ResolverContext resolverContext = initialBatch[i];
                if (resolverContext.Field.Type.IsNonNullType()
                    && result.Data[resolverContext.ResponseName] == null)
                {
                    result.Data.Clear();
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BeginExecuteResolverBatch(
            IReadOnlyList<ResolverContext> batch,
            IErrorHandler errorHandler,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < batch.Count; i++)
            {
                ResolverContext resolverContext = batch[i];
                resolverContext.Task = ExecuteResolverAsync(resolverContext, errorHandler);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static async Task CompleteBatchOperationsAsync(
            IReadOnlyList<ResolverContext> batch,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            Task[] tasks = ExecutionPools.TaskPool.Rent(batch.Count);
            CopyContextTasks(batch, tasks);

            try
            {
                await batchOperationHandler.CompleteAsync(
                    tasks.AsMemory().Slice(0, batch.Count),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                ExecutionPools.TaskPool.Return(tasks);
            }
        }

        private static void CopyContextTasks(
            IReadOnlyList<ResolverContext> batch,
            Span<Task> tasks)
        {
            for (int i = 0; i < batch.Count; i++)
            {
                tasks[i] = batch[i].Task;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task EndExecuteResolverBatchAsync(
            IReadOnlyList<ResolverContext> batch,
            Action<ResolverContext> enqueueNext,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < batch.Count; i++)
            {
                ResolverContext resolverContext = batch[i];

                if (resolverContext.Task.Status != TaskStatus.RanToCompletion)
                {
                    await resolverContext.Task.ConfigureAwait(false);
                }

                ValueCompletion.CompleteValue(enqueueNext, resolverContext);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static List<ResolverContext> CreateInitialBatch(
            IExecutionContext executionContext,
            IDictionary<string, object> result)
        {
            ImmutableStack<object> source = ImmutableStack<object>.Empty
                .Push(executionContext.Operation.RootValue);

            IReadOnlyList<FieldSelection> fieldSelections =
                executionContext.CollectFields(
                    executionContext.Operation.RootType,
                    executionContext.Operation.Definition.SelectionSet,
                    null);

            List<ResolverContext> batch = ExecutionPools.ContextListPool.Get();

            for (int i = 0; i < fieldSelections.Count; i++)
            {
                batch.Add(ResolverContext.Rent(
                    executionContext,
                    fieldSelections[i],
                    source,
                    result));
            }

            return batch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static BatchOperationHandler CreateBatchOperationHandler(
            IExecutionContext executionContext)
        {
            IEnumerable<IBatchOperation> batchOperations =
                executionContext.Services
                    .GetService<IEnumerable<IBatchOperation>>();

            if (batchOperations != null && batchOperations.Any())
            {
                return new BatchOperationHandler(batchOperations);
            }

            return null;
        }
    }
}
