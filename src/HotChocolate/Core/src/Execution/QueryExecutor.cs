using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.DataLoader;
using HotChocolate.Language;

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
            while (!cancellationToken.IsCancellationRequested && 
                !executionContext.IsCompleted)
            {
                while (!cancellationToken.IsCancellationRequested && 
                    executionContext.Tasks.TryDequeue(out ResolverTask task))
                {
                    task.BeginExecute();
                }

                await executionContext.WaitForEngine(cancellationToken).ConfigureAwait(false);

                while (!cancellationToken.IsCancellationRequested && 
                    executionContext.Tasks.IsEmpty && 
                    executionContext.BatchDispatcher.HasTasks)
                {
                    await executionContext.BatchDispatcher.DispatchAsync(cancellationToken)
                        .ConfigureAwait(false);
                    await executionContext.WaitForEngine(cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // ensure non-null propagation
        }
    }
}
