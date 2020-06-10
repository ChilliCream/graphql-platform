using System;
using System.Collections.Generic;
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
                    executionContext.Tasks.TryTake(out ResolverTask? task))
                {
                    task.BeginExecute();
                }

                await executionContext.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task ExecuteResolvers2(
            Channel<ResolverTask> channel,
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested &&
                !channel.Reader.Completion.IsCompleted)
            {
                while (!cancellationToken.IsCancellationRequested &&
                    channel.Reader.TryRead(out ResolverTask? task))
                {
                    task.BeginExecute();
                }

                await channel.Reader.WaitToReadAsync(cancellationToken);
            }
        }
    }
}
