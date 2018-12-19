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
            var data = new OrderedDictionary();

            IEnumerable<ResolverTask> rootResolverTasks =
                CreateRootResolverTasks(executionContext, data);

            await ExecuteResolverBatchSeriallyAsync(
                executionContext, rootResolverTasks,
                cancellationToken);

            return new QueryResult(data, executionContext.GetErrors());
        }

        private async Task ExecuteResolverBatchSeriallyAsync(
           IExecutionContext executionContext,
           IEnumerable<ResolverTask> currentBatch,
           CancellationToken cancellationToken)
        {
            var nextBatch = new List<ResolverTask>();

            foreach (ResolverTask resolverTask in currentBatch)
            {
                if (resolverTask.IsMaxExecutionDepthReached())
                {
                    resolverTask.ReportError(
                        "The field has a depth of " +
                        $"{resolverTask.Path.Depth + 1}," +
                        " which exceeds max allowed depth of " +
                        $"{executionContext.Options.MaxExecutionDepth}");
                }
                else
                {
                    await ExecuteResolverSeriallyAsync(
                        executionContext,
                        resolverTask,
                        nextBatch.Add,
                        cancellationToken);
                }

                // execute child fields with the default parallel flow logic
                await ExecuteResolversAsync(
                    executionContext, nextBatch, cancellationToken);

                nextBatch.Clear();

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private async Task ExecuteResolverSeriallyAsync(
            IExecutionContext executionContext,
            ResolverTask resolverTask,
            Action<ResolverTask> enqueueTask,
            CancellationToken cancellationToken)
        {
            resolverTask.Task = ExecuteResolverAsync(
                resolverTask,
                cancellationToken);

            await CompleteDataLoadersAsync(
                executionContext.DataLoaders,
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
