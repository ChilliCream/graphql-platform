using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Utilities
{
    internal static class ResolverExecutionHelper
    {
        public static Task StartExecutionTaskAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken) =>
            Task.Factory.StartNew(
                async () => await ExecuteResolvers(executionContext, cancellationToken),
                cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        public static async Task ExecuteResolvers(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            await foreach (ResolverTask task in executionContext.TaskStats.Work.Reader
                .ReadAllAsync(cancellationToken))
            {
                task.BeginExecute();
            }
            /*
            while (!cancellationToken.IsCancellationRequested &&
                !executionContext.IsCompleted)
            {
                while (!cancellationToken.IsCancellationRequested &&
                    executionContext.Tasks.TryDequeue(out ResolverTask? task))
                {
                    task.BeginExecute();
                }

                await executionContext.WaitAsync(cancellationToken).ConfigureAwait(false);
            }*/
        }
    }
}
