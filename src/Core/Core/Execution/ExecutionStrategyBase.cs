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
            ____ResolverContext[] initialBatch =
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

                EnsureRootValueNonNullStateAndComplete(
                    executionContext.Result,
                    initialBatch);

                return executionContext.Result;
            }
            finally
            {
                ArrayPool<____ResolverContext>.Shared.Return(initialBatch);
            }
        }

        protected static async Task ExecuteResolversAsync(
            IExecutionContext executionContext,
            IEnumerable<____ResolverContext> initialBatch,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            var batch = new List<____ResolverContext>();
            var next = new List<____ResolverContext>();
            List<____ResolverContext> swap = null;
            bool returnContextObjects = false;

            foreach (____ResolverContext resolverContext in initialBatch)
            {
                if (resolverContext == null)
                {
                    break;
                }
                batch.Add(resolverContext);
            }

            while (batch.Count > 0)
            {
                await ExecuteResolverBatchAsync(
                    batch,
                    next,
                    batchOperationHandler,
                    executionContext.ErrorHandler,
                    cancellationToken)
                    .ConfigureAwait(false);

                //? we could move that to end batch
                if (returnContextObjects)
                {
                    foreach (____ResolverContext resolverContext in batch)
                    {
                        ____ResolverContext.Return(resolverContext);
                    }
                }
                returnContextObjects = true;

                swap = batch;
                batch = next;
                next = swap;
                next.Clear();

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        protected static void EnsureRootValueNonNullStateAndComplete(
            IQueryResult result,
            IEnumerable<____ResolverContext> initialBatch)
        {
            foreach (____ResolverContext resolverContext in initialBatch)
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
                ____ResolverContext.Return(resolverContext);
            }
        }

        private static async Task ExecuteResolverBatchAsync(
            IReadOnlyList<____ResolverContext> batch,
            ICollection<____ResolverContext> next,
            BatchOperationHandler batchOperationHandler,
            IErrorHandler errorHandler,
            CancellationToken cancellationToken)
        {
            // start field resolvers
            BeginExecuteResolverBatch(
                batch,
                errorHandler,
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
        }

        private static void BeginExecuteResolverBatch(
            IReadOnlyCollection<____ResolverContext> batch,
            IErrorHandler errorHandler,
            CancellationToken cancellationToken)
        {
            foreach (____ResolverContext context in batch)
            {
                context.Task = ExecuteResolverAsync(context, errorHandler);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        protected static async Task CompleteBatchOperationsAsync(
            IReadOnlyList<____ResolverContext> batch,
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
                    taskMemory, cancellationToken);
            }
            finally
            {
                ArrayPool<Task>.Shared.Return(tasks);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        private static async Task EndExecuteResolverBatchAsync(
            IEnumerable<____ResolverContext> batch,
            Action<____ResolverContext> enqueueNext,
            CancellationToken cancellationToken)
        {
            var completionContext = new CompleteValueContext2(enqueueNext);

            foreach (____ResolverContext resolverContext in batch)
            {
                await resolverContext.Task.ConfigureAwait(false);
                completionContext.CompleteValue(resolverContext);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        protected static ____ResolverContext[] CreateInitialBatch(
            IExecutionContext executionContext,
            IDictionary<string, object> result)
        {
            ImmutableStack<object> source = ImmutableStack<object>.Empty
                .Push(executionContext.Operation.RootValue);

            IReadOnlyCollection<FieldSelection> fieldSelections =
                executionContext.FieldHelper.CollectFields(
                    executionContext.Operation.RootType,
                    executionContext.Operation.Definition.SelectionSet);

            int i = 0;
            ____ResolverContext[] batch =
                ArrayPool<____ResolverContext>.Shared.Rent(
                    fieldSelections.Count);

            foreach (FieldSelection fieldSelection in fieldSelections)
            {
                batch[i++] = ____ResolverContext.Rent(
                    executionContext,
                    fieldSelection,
                    source,
                    result);
            }

            return batch;
        }

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
