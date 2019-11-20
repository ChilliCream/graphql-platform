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
            ResolverContext[] initialBatch =
                CreateInitialBatch(executionContext,
                    executionContext.Result.Data,
                    out int buffered);
            try
            {
                await ExecuteResolversAsync(
                    executionContext,
                    initialBatch,
                    buffered,
                    batchOperationHandler,
                    cancellationToken)
                    .ConfigureAwait(false);

                EnsureRootValueNonNullState(
                    executionContext.Result,
                    initialBatch.AsSpan().Slice(0, buffered));

                return executionContext.Result;
            }
            finally
            {
                ExecutionPools.ContextPool.Return(initialBatch);
            }
        }

        protected static async Task ExecuteResolversAsync(
            IExecutionContext executionContext,
            ResolverContext[] initialBatch,
            int initialBatchSize,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            ResolverContext[] batchBuffer = ExecutionPools.ContextPool.Rent(initialBatch.Length);
            ResolverContext[] nextBatchBuffer = ExecutionPools.ContextPool.Rent(256);
            ResolverContext[] swap = null;
            int buffered = initialBatchSize;
            int i = 0;

            initialBatch.AsSpan().Slice(0, initialBatchSize).CopyTo(batchBuffer);

            try
            {
                while (buffered > 0)
                {
                    Memory<ResolverContext> batch = batchBuffer.AsMemory().Slice(0, buffered);

                    // start field resolvers
                    BeginExecuteResolverBatch(
                        batch.Span,
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
                        AddItem,
                        cancellationToken)
                        .ConfigureAwait(false);

                    swap = batchBuffer;
                    batchBuffer = nextBatchBuffer;
                    nextBatchBuffer = swap;
                    buffered = i;
                    i = 0;

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                ExecutionPools.ContextPool.Return(batchBuffer);
                ExecutionPools.ContextPool.Return(nextBatchBuffer);
            }

            void AddItem(ResolverContext resolverContext)
            {
                if (nextBatchBuffer.Length <= i)
                {
                    nextBatchBuffer = EnsureBatchCapacity(nextBatchBuffer);
                }
                nextBatchBuffer[i++] = resolverContext;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ResolverContext[] EnsureBatchCapacity(ResolverContext[] buffer)
        {
            ResolverContext[] newBuffer = ExecutionPools.ContextPool.Rent(buffer.Length * 2);
            buffer.AsSpan().CopyTo(newBuffer);
            ExecutionPools.ContextPool.Return(buffer);
            return newBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void EnsureRootValueNonNullState(
            IQueryResult result,
            ReadOnlySpan<ResolverContext> initialBatch)
        {
            for (int i = 0; i < initialBatch.Length; i++)
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
            ReadOnlySpan<ResolverContext> batch,
            IErrorHandler errorHandler,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < batch.Length; i++)
            {
                ResolverContext context = batch[i];
                context.Task = ExecuteResolverAsync(context, errorHandler);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static async Task CompleteBatchOperationsAsync(
            ReadOnlyMemory<ResolverContext> batch,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            Task[] tasks = ExecutionPools.TaskPool.Rent(batch.Length);
            CopyContextTasks(batch.Span, tasks);

            try
            {
                await batchOperationHandler.CompleteAsync(
                    tasks.AsMemory().Slice(0, batch.Length),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                ExecutionPools.TaskPool.Return(tasks);
            }
        }

        private static void CopyContextTasks(
            ReadOnlySpan<ResolverContext> batch,
            Span<Task> tasks)
        {
            for (int i = 0; i < batch.Length; i++)
            {
                tasks[i] = batch[i].Task;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task EndExecuteResolverBatchAsync(
            ReadOnlyMemory<ResolverContext> batch,
            Action<ResolverContext> enqueueNext,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < batch.Length; i++)
            {
                ResolverContext resolverContext = batch.Span[i];

                if (resolverContext.Task.Status != TaskStatus.RanToCompletion)
                {
                    await resolverContext.Task.ConfigureAwait(false);
                }

                ValueCompletion.CompleteValue(enqueueNext, resolverContext);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ResolverContext[] CreateInitialBatch(
            IExecutionContext executionContext,
            IDictionary<string, object> result,
            out int buffered)
        {
            ImmutableStack<object> source = ImmutableStack<object>.Empty
                .Push(executionContext.Operation.RootValue);

            IReadOnlyList<FieldSelection> fieldSelections =
                executionContext.CollectFields(
                    executionContext.Operation.RootType,
                    executionContext.Operation.Definition.SelectionSet,
                    null);

            ResolverContext[] batch = ExecutionPools.ContextPool.Rent(fieldSelections.Count);

            for (int i = 0; i < fieldSelections.Count; i++)
            {
                batch[i] = ResolverContext.Rent(
                    executionContext,
                    fieldSelections[i],
                    source,
                    result);
            }

            buffered = fieldSelections.Count;
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
