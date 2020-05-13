using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class QueryExecutor : IOperationExecutor
    {
        public Task<IExecutionResult> ExecuteAsync(
            IOperationContext executionContext,
            CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        private async Task ExecuteResolversAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            BeginCompletion(executionContext, cancellationToken);

            while (!cancellationToken.IsCancellationRequested && !executionContext.IsCompleted)
            {
                while (!cancellationToken.IsCancellationRequested &&
                    executionContext.Tasks.TryDequeue(out ResolverTask? task))
                {
                    task.BeginExecute();
                }

                await executionContext.WaitForEngine(cancellationToken).ConfigureAwait(false);

                while (!cancellationToken.IsCancellationRequested &&
                    executionContext.Tasks.IsEmpty &&
                    executionContext.BatchDispatcher.HasTasks)
                {
                    executionContext.BatchDispatcher.Dispatch();
                    await executionContext.WaitForEngine(cancellationToken).ConfigureAwait(false);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // ensure non-null propagation
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
                            executionContext.Completion.TryDequeue(out ResolverTask? task))
                        {
                            if (!task.IsCompleted)
                            {
                                await task.EndExecuteAsync().ConfigureAwait(false);
                            }
                            // todo : return task to pool
                        }
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
    }
}
