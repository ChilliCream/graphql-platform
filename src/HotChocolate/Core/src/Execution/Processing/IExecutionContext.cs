using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// The execution context provides access to the task queue,
    /// the batch dispatcher and exposes processing relevant state information.
    /// </summary>
    internal interface IExecutionContext
    {
        /// <summary>
        /// Gets the backlog of the task that have to be processed.
        /// </summary>
        ITaskBacklog TaskBacklog { get; }

        /// <summary>
        /// Gets the backlog of the task that shall be processed after
        /// all the main tasks have been executed.
        /// </summary>
        IDeferredTaskBacklog DeferredTaskBacklog { get; }

        ObjectPool<ResolverTask> TaskPool { get; }

        ITaskStatistics TaskStats { get; }

        IExecutionTaskContext TaskContext { get; }

        /// <summary>
        /// Gets the batch dispatcher.
        /// </summary>
        IBatchDispatcher BatchDispatcher { get; }

        /// <summary>
        /// Gets the context batch dispatcher.
        /// </summary>
        IContextBatchDispatcher ContextBatchDispatcher { get; }

        bool IsCompleted { get; }

        void Reset();
    }
}
