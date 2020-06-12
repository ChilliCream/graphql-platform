using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;

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
        ITaskBacklog TaskBacklog { get; }

        ObjectPool<ResolverTask> TaskPool { get; }

        ITaskStatistics TaskStats { get; }

        /// <summary>
        /// Gets the batch dispatcher.
        /// </summary>
        IBatchDispatcher BatchDispatcher { get; }

        bool IsCompleted { get; }
    }
}
