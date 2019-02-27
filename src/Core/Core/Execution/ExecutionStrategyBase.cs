using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal abstract partial class ExecutionStrategyBase
        : IExecutionStrategy
    {
        public abstract Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken);

        protected static async Task<IQueryResult> ExecuteQueryAsync(
            IExecutionContext executionContext,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            IEnumerable<ResolverTask> rootResolverTasks =
                CreateRootResolverTasks(executionContext,
                    executionContext.Result.Data);

            await ExecuteResolversAsync(
                executionContext,
                rootResolverTasks,
                batchOperationHandler,
                cancellationToken)
                    .ConfigureAwait(false);

            EnsureRootValueNonNullState(
                executionContext.Result,
                rootResolverTasks);

            return executionContext.Result;
        }

        protected static async Task ExecuteResolversAsync(
            IExecutionContext executionContext,
            IEnumerable<ResolverTask> initialBatch,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            var currentBatch = new List<ResolverTask>(initialBatch);
            var nextBatch = new List<ResolverTask>();
            List<ResolverTask> swap = null;

            while (currentBatch.Count > 0)
            {
                await ExecuteResolverBatchAsync(
                    executionContext, currentBatch, nextBatch,
                    batchOperationHandler, cancellationToken)
                        .ConfigureAwait(false);

                swap = currentBatch;
                currentBatch = nextBatch;
                nextBatch = swap;
                nextBatch.Clear();

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        protected static void EnsureRootValueNonNullState(
            IQueryResult result,
            IEnumerable<ResolverTask> rootResolverTasks)
        {
            foreach (ResolverTask resolverTask in rootResolverTasks)
            {
                FieldSelection selection = resolverTask.FieldSelection;
                if (resolverTask.FieldType.IsNonNullType()
                    && result.Data[selection.ResponseName] == null)
                {
                    result.Data.Clear();
                    break;
                }
            }
        }

        private static async Task ExecuteResolverBatchAsync(
            IExecutionContext executionContext,
            IReadOnlyCollection<ResolverTask> currentBatch,
            ICollection<ResolverTask> nextBatch,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            // start field resolvers
            IReadOnlyCollection<Task> tasks = BeginExecuteResolverBatch(
                currentBatch,
                executionContext.ErrorHandler,
                cancellationToken);

            // execute batch data loaders
            await CompleteBatchOperationsAsync(
                tasks,
                batchOperationHandler,
                cancellationToken)
                    .ConfigureAwait(false);

            // await field resolver results
            await EndExecuteResolverBatchAsync(
                executionContext,
                currentBatch,
                nextBatch.Add,
                cancellationToken)
                    .ConfigureAwait(false);
        }

        private static IReadOnlyCollection<Task> BeginExecuteResolverBatch(
            IEnumerable<ResolverTask> currentBatch,
            IErrorHandler errorHandler,
            CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            foreach (ResolverTask resolverTask in currentBatch)
            {
                resolverTask.Task = ExecuteResolverAsync(
                    resolverTask,
                    errorHandler,
                    cancellationToken);
                tasks.Add(resolverTask.Task);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return tasks;
        }

        protected static Task CompleteBatchOperationsAsync(
            IReadOnlyCollection<Task> tasks,
            BatchOperationHandler batchOperationHandler,
            CancellationToken cancellationToken)
        {
            return (batchOperationHandler == null)
                ? Task.CompletedTask
                : batchOperationHandler.CompleteAsync(
                    tasks, cancellationToken);
        }

        private static async Task EndExecuteResolverBatchAsync(
            IExecutionContext executionContext,
            IEnumerable<ResolverTask> currentBatch,
            Action<ResolverTask> enqueueTask,
            CancellationToken cancellationToken)
        {
            var completionContext = new CompleteValueContext(
                executionContext.FieldHelper, enqueueTask);

            foreach (ResolverTask resolverTask in currentBatch)
            {
                resolverTask.ResolverResult = await resolverTask.Task
                    .ConfigureAwait(false);

                completionContext.CompleteValue(resolverTask);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        protected static IReadOnlyList<ResolverTask> CreateRootResolverTasks(
            IExecutionContext executionContext,
            IDictionary<string, object> result)
        {
            var tasks = new List<ResolverTask>();

            ImmutableStack<object> source = ImmutableStack<object>.Empty
                .Push(executionContext.Operation.RootValue);

            IReadOnlyCollection<FieldSelection> fieldSelections =
                executionContext.FieldHelper.CollectFields(
                    executionContext.Operation.RootType,
                    executionContext.Operation.Definition.SelectionSet);

            foreach (FieldSelection fieldSelection in fieldSelections)
            {
                tasks.Add(new ResolverTask(
                    executionContext,
                    fieldSelection,
                    source,
                    result));
            }

            return tasks;
        }

        protected static BatchOperationHandler CreateBatchOperationHandler(
            IExecutionContext executionContext)
        {
            IEnumerable<IBatchOperation> batchOperations =
                executionContext.Services
                    .GetService<IEnumerable<IBatchOperation>>();

            if (batchOperations != null && batchOperations.Any())
            {
                return new BatchOperationHandler(batchOperations);
            }

            return null;
        }
    }
}
