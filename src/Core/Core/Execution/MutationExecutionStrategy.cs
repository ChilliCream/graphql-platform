using System;
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
            ResolverContext[] initialBatchBuffer =
                CreateInitialBatch(executionContext,
                    executionContext.Result.Data,
                    out int buffered);

            BatchOperationHandler batchOperationHandler =
                CreateBatchOperationHandler(executionContext);

            try
            {
                Memory<ResolverContext> initialBatch = initialBatchBuffer.AsMemory(0, buffered);

                await ExecuteResolverBatchSeriallyAsync(
                    executionContext,
                    initialBatch,
                    batchOperationHandler,
                    cancellationToken)
                    .ConfigureAwait(false);

                EnsureRootValueNonNullState(
                    executionContext.Result,
                    initialBatch.Span);

                return executionContext.Result;
            }
            finally
            {
                batchOperationHandler?.Dispose();
                ReleaseTrackedContextObjects(executionContext);
                ExecutionPools.ContextPool.Return(initialBatchBuffer);
            }
        }

        private static async Task ExecuteResolverBatchSeriallyAsync(
            IExecutionContext executionContext,
            ReadOnlyMemory<ResolverContext> batch,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < batch.Length; i++)
            {
                // note: this buffer will be returned by ExecuteResolversAsync
                ResolverContext[] nextBatchBuffer = ExecutionPools.ContextPool.Rent(256);
                int buffered = 0;

                try
                {
                    ResolverContext resolverContext = batch.Span[i];

                    await ExecuteResolverSeriallyAsync(
                        resolverContext,
                        AddItem,
                        batchOperationHandler,
                        executionContext.ErrorHandler,
                        cancellationToken)
                        .ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    // execute child fields with the default parallel flow logic
                    await ExecuteResolversAsync(
                        executionContext,
                        nextBatchBuffer,
                        buffered,
                        batchOperationHandler,
                        cancellationToken)
                        .ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();
                }
                finally
                {
                    ExecutionPools.ContextPool.Return(nextBatchBuffer);
                }

                void AddItem(ResolverContext context)
                {
                    if (nextBatchBuffer.Length <= buffered)
                    {
                        nextBatchBuffer = EnsureBatchCapacity(nextBatchBuffer);
                    }
                    nextBatchBuffer[buffered++] = context;
                }
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
