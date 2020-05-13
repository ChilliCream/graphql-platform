using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    /// <summary>
    /// The execution context provides access to the task queue, 
    /// the batch dispatcher and exposes processing relevant state information. 
    /// </summary>
    internal interface IExecutionContext
    {
        /// <summary>
        /// Gets the task queue.
        /// </summary>
        ITaskQueue Tasks { get; }

        /// <summary>
        /// Gets the tasks that need to be completed.
        /// </summary>
        ICompletionQueue Completion { get; }

        /// <summary>
        /// Gets the batch dispatcher.
        /// </summary>
        IBatchDispatcher BatchDispatcher { get; }

        /// <summary>
        /// wait for => executionContext.Tasks.Count > 0 
        /// || executionContext.BatchDispatcher.HasTasks 
        /// || IsCompleted 
        /// || cancellationToken.IsCancellationRequested
        /// </summary>
        Task WaitForEngine(CancellationToken cancellationToken);

        Task WaitForCompletion(CancellationToken cancellationToken);

        /// <summary>
        /// operationContext.Tasks.IsEmpty 
        /// && operationContext.BatchScheduler.IsEmpty 
        /// && AllTasksDone
        /// </summary>
        bool IsCompleted { get; }
    }
}