using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    /// <summary>
    /// This execution strategy executes the full query graph serailly.
    /// This execution strategy is used to help with entity framework and
    /// will be removed with version 11.
    /// </summary>
    internal sealed class SerialExecutionStrategy
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

            return ExecuteSeriallyAsync(executionContext, cancellationToken);
        }

        private static async Task<IExecutionResult> ExecuteSeriallyAsync(
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
            var current = new List<ResolverContext>(batch);
            var next = new List<ResolverContext>();

            while (current.Count > 0)
            {
                foreach (ResolverContext resolverContext in current)
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

                ResolverContext.Return(current);

                current.Clear();
                current.AddRange(next);
                next.Clear();

                cancellationToken.ThrowIfCancellationRequested();
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
