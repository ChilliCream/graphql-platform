using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    internal static class ResolverExecutionHelper
    {
        public static async Task ExecuteTasksAsync(
            IOperationContext operationContext)
        {
            var proposedTaskCount = operationContext.Operation.ProposedTaskCount;
            var tasks = new Task[proposedTaskCount];

            for (var i = 0; i < proposedTaskCount; i++)
            {
                tasks[i] = StartExecutionTaskAsync(
                    operationContext.Execution,
                    operationContext.RequestAborted);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static Task StartExecutionTaskAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken) =>
            Task.Run(() => ExecuteResolvers(executionContext, cancellationToken));

        private static async Task ExecuteResolvers(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested &&
                !executionContext.IsCompleted)
            {
                while (!cancellationToken.IsCancellationRequested &&
                    executionContext.TaskBacklog.TryTake(out IExecutionTask? task))
                {
                    task.BeginExecute();
                }

                await executionContext.TaskBacklog
                    .WaitForTaskAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
