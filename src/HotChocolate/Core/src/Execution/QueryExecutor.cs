using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        private async Task ExecuteResolversAsync(IOperationContext operationContext)
        {
            // operationContext.Tasks.IsEmpty && operationContext.BatchScheduler.IsEmpty && AllTasksDone
            while (!operationContext.IsCompleted)
            {
                while (operationContext.TaskQueue.HasTasks)
                {
                    ResolverTask task = operationContext.TaskQueue.Dequeue();
                    task.BeginExecute();
                }

                // operationContext.Tasks.HasTasks || operationContext.BatchScheduler.HasTasks
                await operationContext.WaitForEngine();

                while (operationContext.TaskQueue.IsEmpty && operationContext.BatchScheduler.HasTasks)
                {
                    await operationContext.BatchScheduler.DispatchAsync(
                        operationContext.RequestAborted)
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

    internal interface IBatchScheduler
    {
        bool HasTasks { get; }

        bool IsEmpty { get; }

        Task DispatchAsync(CancellationToken cancellationToken);
    }

    internal interface INonNullViolationTracker
    {
        void Register(FieldNode selection, IResultData resultData);
    }
}
