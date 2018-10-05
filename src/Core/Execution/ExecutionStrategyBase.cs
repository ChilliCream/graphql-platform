using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using HotChocolate.Types;

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
            IEnumerable<ResolverTask> initialBatch,
            CancellationToken cancellationToken)
        {
            var currentBatch = new List<ResolverTask>(initialBatch);
            var nextBatch = new List<ResolverTask>();
            List<ResolverTask> swap = null;

            while (currentBatch.Count > 0)
            {
                await ExecuteResolverBatchAsync(executionContext,
                    currentBatch, nextBatch, cancellationToken);

                swap = currentBatch;
                currentBatch = nextBatch;
                nextBatch = swap;
                nextBatch.Clear();

                cancellationToken.ThrowIfCancellationRequested();
            }
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

            // execute batch data loaders
            await CompleteDataLoadersAsync(
                executionContext.DataLoaders,
                cancellationToken);

            // await field resolver results
            await EndExecuteResolverBatchAsync(
                executionContext, currentBatch, nextBatch, cancellationToken);
        }

        private void BeginExecuteResolverBatch(
            IExecutionContext executionContext,
            IEnumerable<ResolverTask> currentBatch,
            CancellationToken cancellationToken)
        {
            foreach (ResolverTask resolverTask in currentBatch)
            {
                bool isLeafField =
                    resolverTask.FieldSelection.Field.Type.IsLeafType();

                if (IsMaxExecutionDepthReached(executionContext, resolverTask))
                {
                    executionContext.ReportError(resolverTask.CreateError(
                        "The field has a depth of " +
                        $"{resolverTask.Path.Depth}, " +
                        "which exceeds max allowed depth of " +
                        $"{executionContext.Options.MaxExecutionDepth}"));
                }
                else
                {
                    resolverTask.ResolverResult = ExecuteResolver(
                        resolverTask, executionContext.Options.DeveloperMode,
                        cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        protected async Task CompleteDataLoadersAsync(
            IDataLoaderProvider dataLoaders,
            CancellationToken cancellationToken)
        {
            if (dataLoaders != null)
            {
                await Task.WhenAll(dataLoaders.Touched
                    .Select(t => t.TriggerAsync(cancellationToken)));
                dataLoaders.Reset();
            }
        }

        private async Task EndExecuteResolverBatchAsync(
            IExecutionContext executionContext,
            IEnumerable<ResolverTask> currentBatch,
            List<ResolverTask> nextBatch,
            CancellationToken cancellationToken)
        {
            foreach (ResolverTask resolverTask in currentBatch)
            {
                // await async results
                resolverTask.ResolverResult = await FinalizeResolverResultAsync(
                    resolverTask.FieldSelection.Selection,
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

        protected IEnumerable<ResolverTask> CreateRootResolverTasks(
            IExecutionContext executionContext,
            OrderedDictionary result)
        {
            ImmutableStack<object> source = ImmutableStack<object>.Empty
                .Push(executionContext.RootValue);

            IReadOnlyCollection<FieldSelection> fieldSelections =
                executionContext.CollectFields(
                    executionContext.OperationType,
                    executionContext.Operation.SelectionSet);

            foreach (FieldSelection fieldSelection in fieldSelections)
            {
                yield return new ResolverTask(
                    executionContext,
                    executionContext.OperationType,
                    fieldSelection,
                    Path.New(fieldSelection.ResponseName),
                    source,
                    result);
            }
        }
    }
}
