using System.Diagnostics;
using System.Buffers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
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
                    executionContext.Result.Data);
            try
            {
                await ExecuteResolversAsync(
                    executionContext,
                    initialBatch,
                    batchOperationHandler,
                    cancellationToken)
                    .ConfigureAwait(false);

                EnsureRootValueNonNullState(
                    executionContext.Result,
                    initialBatch);

                return executionContext.Result;
            }
            finally
            {
                ResolverContext.Return(initialBatch);
                ArrayPool<ResolverContext>.Shared.Return(initialBatch);
            }
        }

        protected static async Task ExecuteResolversAsync(
            IExecutionContext executionContext,
            IEnumerable<ResolverContext> initialBatch,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            var batch = new List<ResolverContext>();
            var next = new List<ResolverContext>();
            List<ResolverContext> swap = null;

            foreach (ResolverContext resolverContext in initialBatch)
            {
                if (resolverContext == null)
                {
                    break;
                }
                batch.Add(resolverContext);
            }

            while (batch.Count > 0)
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

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void EnsureRootValueNonNullState(
            IQueryResult result,
            IEnumerable<ResolverContext> initialBatch)
        {
            foreach (ResolverContext resolverContext in initialBatch)
            {
                if (resolverContext is null)
                {
                    break;
                }

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
            IReadOnlyCollection<ResolverContext> batch,
            IErrorHandler errorHandler,
            CancellationToken cancellationToken)
        {
            foreach (ResolverContext context in batch)
            {
                context.Task = ExecuteResolverAsync(context, errorHandler);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static async Task CompleteBatchOperationsAsync(
            IReadOnlyList<ResolverContext> batch,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            Debug.Assert(batch != null);
            Debug.Assert(batchOperationHandler != null);

            Task[] tasks = ArrayPool<Task>.Shared.Rent(batch.Count);
            for (int i = 0; i < batch.Count; i++)
            {
                tasks[i] = batch[i].Task;
            }

            var taskMemory = new Memory<Task>(tasks);
            taskMemory = taskMemory.Slice(0, batch.Count);

            try
            {
                await batchOperationHandler.CompleteAsync(
                    taskMemory, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<Task>.Shared.Return(tasks);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task EndExecuteResolverBatchAsync(
            IEnumerable<ResolverContext> batch,
            Action<ResolverContext> enqueueNext,
            CancellationToken cancellationToken)
        {
            foreach (ResolverContext resolverContext in batch)
            {
                if (resolverContext.Task.Status != TaskStatus.RanToCompletion)
                {
                    await resolverContext.Task.ConfigureAwait(false);
                }

                ValueCompletion.CompleteValue(enqueueNext, resolverContext);

                if (!resolverContext.IsRoot)
                {
                    ResolverContext.Return(resolverContext);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ResolverContext[] CreateInitialBatch(
            IExecutionContext executionContext,
            IDictionary<string, object> result)
        {
            ImmutableStack<object> source = ImmutableStack<object>.Empty
                .Push(executionContext.Operation.RootValue);

            IReadOnlyCollection<FieldSelection> fieldSelections =
                executionContext.CollectFields(
                    executionContext.Operation.RootType,
                    executionContext.Operation.Definition.SelectionSet,
                    null);

            int i = 0;
            ResolverContext[] batch =
                ArrayPool<ResolverContext>.Shared.Rent(
                    fieldSelections.Count);

            foreach (FieldSelection fieldSelection in fieldSelections)
            {
                batch[i++] = ResolverContext.Rent(
                    executionContext,
                    fieldSelection,
                    source,
                    result);
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
