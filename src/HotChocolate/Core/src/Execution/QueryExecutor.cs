using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class QueryExecutor : IOperationExecutor
    {
        public async Task<IExecutionResult> ExecuteAsync(
            IOperationContext operationContext,
            CancellationToken cancellationToken)
        {
            var scopedContext = ImmutableDictionary<string, object?>.Empty;
            IPreparedSelectionList rootSelections = operationContext.Operation.GetRootSelections();
            ResultMap resultMap = operationContext.Result.RentResultMap(rootSelections.Count);
            int responseIndex = 0;

            for (int i = 0; i < rootSelections.Count; i++)
            {
                IPreparedSelection selection = rootSelections[i];
                if (selection.IsVisible(operationContext.Variables))
                {
                    operationContext.Execution.Tasks.Enqueue(
                        selection,
                        responseIndex++,
                        resultMap,
                        operationContext.RootValue,
                        Path.New(selection.ResponseName),
                        scopedContext);
                }
            }

            await ExecuteResolversAsync(
                operationContext.Execution, 
                cancellationToken)
                .ConfigureAwait(false);

            operationContext.Result.SetData(resultMap);
            return operationContext.Result.BuildResult();
        }

        private async Task ExecuteResolversAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            BeginCompletion(executionContext, cancellationToken);

            while (!cancellationToken.IsCancellationRequested && !executionContext.IsCompleted)
            {
                while (!cancellationToken.IsCancellationRequested &&
                    !executionContext.IsCompleted &&
                    executionContext.Tasks.TryDequeue(out ResolverTask? task))
                {
                    task.BeginExecute();
                }

                await executionContext.WaitForEngine(cancellationToken).ConfigureAwait(false);

                while (!cancellationToken.IsCancellationRequested &&
                    !executionContext.IsCompleted &&
                    executionContext.Tasks.IsEmpty &&
                    executionContext.BatchDispatcher.HasTasks)
                {
                    executionContext.BatchDispatcher.Dispatch();
                    await executionContext.WaitForEngine(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Completes running resolver tasks and returns task to the bool.
        /// </summary>
        private void BeginCompletion(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(
                async () =>
                {
                    while (!cancellationToken.IsCancellationRequested &&
                        !executionContext.IsCompleted)
                    {
                        await executionContext.WaitForCompletion(cancellationToken)
                            .ConfigureAwait(false);

                        while (!cancellationToken.IsCancellationRequested &&
                            executionContext.RunningTasks.TryDequeue(out ResolverTask? task))
                        {
                            if (!task.IsCompleted)
                            {
                                await task.EndExecuteAsync().ConfigureAwait(false);
                            }
                            executionContext.TaskPool.Return(task);
                        }
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
    }
}
