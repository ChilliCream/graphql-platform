using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Utilities
{
    internal static class ResolverExecutionHelper 
    {
        public static async Task ExecuteResolversAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested &&
                !executionContext.IsCompleted)
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
    }
}
