using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Utilities
{
    internal static class ResolverExecutionHelper
    {
        public static Task StartExecutionTaskAsync(
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
                    executionContext.TaskBacklog.TryTake(out ITask? task))
                {
                    task.BeginExecute();
                }

                await executionContext.TaskBacklog.WaitForTaskAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
