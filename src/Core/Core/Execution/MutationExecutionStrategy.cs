using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal class MutationExecutionStrategy
        : ExecutionStrategyBase
    {
        public override Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            return ExecuteMutationAsync(executionContext, cancellationToken);
        }

        private static async Task<IExecutionResult> ExecuteMutationAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            List<ResolverContext> initialBatch =
                CreateInitialBatch(
                    executionContext,
                    executionContext.Result.Data);

            BatchOperationHandler batchOperationHandler =
                CreateBatchOperationHandler(executionContext);

            try
            {
                await ExecuteResolverBatchSeriallyAsync(
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
                batchOperationHandler?.Dispose();
                ReleaseTrackedContextObjects(executionContext);
                ExecutionPools.ResolverContextList.Return(initialBatch);
            }
        }

        private static async Task ExecuteResolverBatchSeriallyAsync(
            IExecutionContext executionContext,
            IReadOnlyList<ResolverContext> batch,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            List<ResolverContext> next = ExecutionPools.ResolverContextList.Get();

            try
            {
                for (int i = 0; i < batch.Count; i++)
                {
                    if (i != 0)
                    {
                        next.Clear();
                    }

                    ResolverContext resolverContext = batch[i];

                    await ExecuteResolverSeriallyAsync(
                        resolverContext,
                        next.Add,
                        batchOperationHandler,
                        executionContext.ErrorHandler,
                        cancellationToken)
                        .ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    // execute child fields with the default parallel flow logic
                    await ExecuteResolversAsync(
                        executionContext,
                        next,
                        batchOperationHandler,
                        cancellationToken)
                        .ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                ExecutionPools.ResolverContextList.Return(next);
            }
        }

        private static async Task ExecuteResolverSeriallyAsync(
            ResolverContext resolverContext,
            Action<ResolverContext> enqueueNext,
            BatchOperationHandler batchOperationHandler,
            IErrorHandler errorHandler,
            CancellationToken cancellationToken)
        {
            resolverContext.Task = ExecuteResolverAsync(
                resolverContext,
                errorHandler);

            if (batchOperationHandler != null)
            {
                await CompleteBatchOperationsAsync(
                    new[] { resolverContext },
                    batchOperationHandler,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            await resolverContext.Task.ConfigureAwait(false);

            // serialize and integrate result into final query result
            ValueCompletion.CompleteValue(enqueueNext, resolverContext);
        }
    }
}
