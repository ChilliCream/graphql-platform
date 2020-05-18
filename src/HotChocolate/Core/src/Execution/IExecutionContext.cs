using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;
using HotChocolate.Execution.Utilities;
using Microsoft.Extensions.ObjectPool;

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

        ObjectPool<ResolverTask> TaskPool { get; }

        ITaskStatistics TaskStats { get; }

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

        /// <summary>
        /// operationContext.TaskStats.Enqueued == 0
        /// && operationContext.TaskStats.Running == 0
        /// </summary>
        bool IsCompleted { get; }
    }
}
