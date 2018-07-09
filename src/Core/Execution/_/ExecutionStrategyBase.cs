using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal abstract partial class ExecutionStrategyBase
        : IExecutionStrategy
    {
        public abstract Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken);


        protected async Task ExecuteResolversAsync(
            IExecutionContext executionContext,
            List<ResolverTask> resolverTasks,
            CancellationToken cancellationToken)
        {
            /*
            while (resolverTasks.Count > 0)
            {
                List<FieldResolverTask> currentBatch =
                    new List<FieldResolverTask>(executionContext.NextBatch);
                executionContext.NextBatch.Clear();

                await ExecuteResolverBatchAsync(executionContext,
                    currentBatch, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
            }
             */
            throw new NotImplementedException();
        }

        private async Task ExecuteResolverBatchAsync(
            IExecutionContext executionContext,
            IReadOnlyCollection<ResolverTask> currentBatch,
            List<ResolverTask> nextBatch,
            CancellationToken cancellationToken)
        {
            // start field resolvers
            BeginExecuteResolverBatch(
                executionContext, currentBatch, cancellationToken);

            // await field resolver results
            await EndResolverBatchAsync(
                executionContext, currentBatch, nextBatch, cancellationToken);
        }

        private void BeginExecuteResolverBatch(
            IExecutionContext executionContext,
            IEnumerable<ResolverTask> currentBatch,
            CancellationToken cancellationToken)
        {
            foreach (ResolverTask resolverTask in currentBatch)
            {
                if (resolverTask.Path.Depth <= executionContext.Options.MaxExecutionDepth)
                {
                    /*
                    object resolverResult = ExecuteFieldResolver(
                        resolverContext, task.FieldSelection.Field,
                        task.FieldSelection.Node, cancellationToken);
                    runningTasks.Add((task, resolverContext, resolverResult));
                    */
                }
                else
                {
                    executionContext.ReportError(resolverTask.CreateError(
                        $"The field has a depth of {resolverTask.Path.Depth}, " +
                        "which exceeds max allowed depth of " +
                        $"{executionContext.Options.MaxExecutionDepth}"));
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private async Task EndResolverBatchAsync(
            IExecutionContext executionContext,
            IEnumerable<ResolverTask> currentBatch,
            List<ResolverTask> nextBatch,
            CancellationToken cancellationToken)
        {
            foreach (ResolverTask resolverTask in currentBatch)
            {
                // await async results
                resolverTask.ResolverResult = await FinalizeResolverResultAsync(
                    resolverTask.FieldSelection.Node,
                    resolverTask.ResolverResult,
                    executionContext.Options.DeveloperMode);

                // serialize and integrate result into final query result
                var completionContext = new FieldValueCompletionContext(
                    executionContext, resolverTask.ResolverContext,
                    resolverTask, t => nextBatch.Add(t));

                CompleteValue(completionContext);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
