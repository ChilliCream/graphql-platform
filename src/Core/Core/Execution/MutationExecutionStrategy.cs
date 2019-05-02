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

        private async Task<IExecutionResult> ExecuteMutationAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            ____ResolverContext[] initialBatch =
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

                EnsureRootValueNonNullStateAndComplete(
                    executionContext.Result,
                    initialBatch);

                return executionContext.Result;
            }
            finally
            {
                batchOperationHandler?.Dispose();
                ArrayPool<____ResolverContext>.Shared.Return(initialBatch);
            }
        }

        private async Task ExecuteResolverBatchSeriallyAsync(
           IExecutionContext executionContext,
           IEnumerable<____ResolverContext> batch,
           BatchOperationHandler batchOperationHandler,
           CancellationToken cancellationToken)
        {
            var next = new List<____ResolverContext>();

            foreach (____ResolverContext resolverContext in batch)
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
            }

            // execute child fields with the default parallel flow logic
            await ExecuteResolversAsync(
                executionContext,
                next,
                batchOperationHandler,
                cancellationToken)
                .ConfigureAwait(false);

            foreach (____ResolverContext resolverContext in next)
            {
                ____ResolverContext.Return(resolverContext);
            }
        }

        private async Task ExecuteResolverSeriallyAsync(
            ____ResolverContext resolverContext,
            Action<____ResolverContext> enqueueNext,
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
            var completionContext = new CompleteValueContext2(enqueueNext);
            completionContext.CompleteValue(resolverContext);
        }
    }
}
