using System.Diagnostics;
using System.Buffers;
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

        protected static async Task<IReadOnlyQueryResult> ExecuteQueryAsync(
            IExecutionContext executionContext,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            InitialBatch initialBatch = CreateInitialBatch(executionContext);

            try
            {
                await ExecuteResolversAsync(
                    executionContext,
                    initialBatch.Batch,
                    batchOperationHandler,
                    cancellationToken)
                    .ConfigureAwait(false);

                FieldData data = EnsureRootValueNonNullState(
                    initialBatch.Data,
                    initialBatch.Batch);

                if (data is { })
                {
                    executionContext.Result.SetData(initialBatch.Data);
                }

                return executionContext.Result.Create();
            }
            finally
            {
                ResolverContext.Return(initialBatch.Batch);
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
        protected static FieldData EnsureRootValueNonNullState(
            FieldData data,
            IEnumerable<ResolverContext> initialBatch)
        {
            foreach (ResolverContext resolverContext in initialBatch)
            {
                if (resolverContext is null)
                {
                    break;
                }

                if (resolverContext.Field.Type.IsNonNullType()
                    && data.GetFieldValue(resolverContext.ResponseIndex) is null)
                {
                    data.Clear();
                    return null;
                }
            }

            return data;
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
        protected static InitialBatch CreateInitialBatch(IExecutionContext executionContext)
        {
            ImmutableStack<object> source = ImmutableStack<object>.Empty
                .Push(executionContext.Operation.RootValue);

            IReadOnlyList<FieldSelection> fieldSelections =
                executionContext.CollectFields(
                    executionContext.Operation.RootType,
                    executionContext.Operation.Definition.SelectionSet,
                    null);

            var data = new FieldData(fieldSelections.Count);
            var batch = new ResolverContext[fieldSelections.Count];

            for (int i = 0; i < fieldSelections.Count; i++)
            {
                batch[i] = ResolverContext.Rent(
                    executionContext,
                    fieldSelections[i],
                    source,
                    data);
            }

            return new InitialBatch(data, batch);
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

        public readonly struct InitialBatch
        {
            public InitialBatch(FieldData data, ResolverContext[] batch)
            {
                Data = data;
                Batch = batch;
            }

            public FieldData Data { get; }

            public ResolverContext[] Batch { get; }
        }
    }
}
