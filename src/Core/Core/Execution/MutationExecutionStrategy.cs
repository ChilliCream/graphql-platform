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

        private async Task<IExecutionResult> ExecuteMutationAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            BatchOperationHandler batchOperationHandler =
                CreateBatchOperationHandler(executionContext);

            try
            {
                IEnumerable<ResolverTask> rootResolverTasks =
                    CreateRootResolverTasks(executionContext,
                        executionContext.Result.Data);

                await ExecuteResolverBatchSeriallyAsync(
                    executionContext,
                    rootResolverTasks,
                    batchOperationHandler,
                    cancellationToken)
                    .ConfigureAwait(false);

                return executionContext.Result;
            }
            finally
            {
                batchOperationHandler?.Dispose();
            }
        }

        private async Task ExecuteResolverBatchSeriallyAsync(
           IExecutionContext executionContext,
           IEnumerable<ResolverTask> currentBatch,
           BatchOperationHandler batchOperationHandler,
           CancellationToken cancellationToken)
        {
            var nextBatch = new List<ResolverTask>();

            foreach (ResolverTask resolverTask in currentBatch)
            {
                await ExecuteResolverSeriallyAsync(
                    executionContext,
                    resolverTask,
                    nextBatch.Add,
                    batchOperationHandler,
                    cancellationToken)
                    .ConfigureAwait(false);

                // execute child fields with the default parallel flow logic
                await ExecuteResolversAsync(
                    executionContext,
                    nextBatch,
                    batchOperationHandler,
                    cancellationToken)
                    .ConfigureAwait(false);

                nextBatch.Clear();

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private async Task ExecuteResolverSeriallyAsync(
            IExecutionContext executionContext,
            ResolverTask resolverTask,
            Action<ResolverTask> enqueueTask,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            resolverTask.Task = ExecuteResolverAsync(
                resolverTask,
                executionContext.ErrorHandler,
                cancellationToken);

            await CompleteBatchOperationsAsync(
                new[] { resolverTask.Task },
                batchOperationHandler,
                cancellationToken);

            if (resolverTask.Task.IsCompleted)
            {
                resolverTask.ResolverResult = resolverTask.Task.Result;
            }
            else
            {
                resolverTask.ResolverResult = await resolverTask.Task;
            }

            // serialize and integrate result into final query result
            var completionContext = new FieldValueCompletionContext(
                executionContext, resolverTask.ResolverContext,
                resolverTask, enqueueTask);

            CompleteValue(completionContext);
        }
    }
}
