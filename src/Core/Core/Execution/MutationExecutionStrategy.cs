using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;

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
            ResolverContext[] initialBatch =
                    CreateInitialBatch(executionContext,
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
                ResolverContext.Return(initialBatch);
                ArrayPool<ResolverContext>.Shared.Return(initialBatch);
            }
        }

        private static async Task ExecuteResolverBatchSeriallyAsync(
           IExecutionContext executionContext,
           IEnumerable<ResolverContext> batch,
           BatchOperationHandler batchOperationHandler,
           CancellationToken cancellationToken)
        {
            var next = new List<ResolverContext>();

            foreach (ResolverContext resolverContext in batch)
            {
                if (resolverContext is null)
                {
                    break;
                }

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

                ResolverContext.Return(next);
                next.Clear();
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
