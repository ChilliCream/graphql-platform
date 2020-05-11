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
            IOperationContext operationContext,
            IBatchDispatcher batchDispatcher, // we might have more than one
            ITaskQueue taskQueue)
        {
            // operationContext.Tasks.IsEmpty && operationContext.BatchScheduler.IsEmpty && AllTasksDone
            while (!operationContext.IsCompleted)
            {
                while (operationContext.TaskQueue.HasTasks)
                {
                    ResolverTask task = operationContext.TaskQueue.Dequeue();
                    task.BeginExecute();
                }

                while (taskQueue.IsEmpty && batchDispatcher.HasTasks)
                {
                    await batchDispatcher.DispatchAsync(operationContext.RequestAborted)
                        .ConfigureAwait(false);
                    await operationContext.WaitForEngine();
                }
            }

            // ensure non-null propagation
        }
    }

    internal interface ITaskQueue
    {
        bool HasTasks { get; }

        bool IsEmpty { get; }

        ResolverTask Dequeue();

        void Enqueue(ResolverTask task);
    }

    internal static class ValueCompletion
    {
        void Register(FieldNode selection, IResultData resultData);
    }
}
