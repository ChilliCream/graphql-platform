using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal class MutationExecutionStrategy
        : ExecutionStrategyBase
    {
        public override async Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

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
            foreach (ResolverTask resolverTask in currentBatch)
            {
                var nextBatch = new List<ResolverTask>();

                if (resolverTask.Path.Depth <= executionContext.Options.MaxExecutionDepth)
                {
                    await ExecuteResolverSeriallyAsync(
                        executionContext, resolverTask,
                        nextBatch, cancellationToken);
                }
                else
                {
                    executionContext.ReportError(resolverTask.CreateError(
                        $"The field has a depth of {resolverTask.Path.Depth}, " +
                        "which exceeds max allowed depth of " +
                        $"{executionContext.Options.MaxExecutionDepth}"));
                }

                // execute child fields with the default parallel flow logic
                await ExecuteResolversAsync(
                    executionContext, nextBatch, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private async Task ExecuteResolverSeriallyAsync(
            IExecutionContext executionContext,
            ResolverTask resolverTask,
            List<ResolverTask> nextBatch,
            CancellationToken cancellationToken)
        {
            resolverTask.ResolverResult = ExecuteResolver(
                resolverTask, executionContext.Options.DeveloperMode,
                cancellationToken);

            if (executionContext.DataLoaders != null)
            {
                await CompleteDataLoadersAsync(
                    executionContext.DataLoaders,
                    cancellationToken);
            }

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
        }
    }
}
