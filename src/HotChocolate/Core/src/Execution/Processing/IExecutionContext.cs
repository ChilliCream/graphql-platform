using System.Threading.Tasks;
using HotChocolate.Execution.Processing.Internal;
using HotChocolate.Execution.Processing.Plan;
using HotChocolate.Execution.Processing.Tasks;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Fetching;

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
        IWorkBacklog Work { get; }

        /// <summary>
        /// Gets the backlog of the task that shall be processed after
        /// all the main tasks have been executed.
        /// </summary>
        IDeferredWorkBacklog DeferredWork { get; }

        /// <summary>
        /// Gets the batch dispatcher.
        /// </summary>
        IBatchDispatcher BatchDispatcher { get; }

        /// <summary>
        /// Defines that the execution is completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Gets the resolver task pool.
        /// </summary>
        ObjectPool<ResolverTask> ResolverTasks { get; }

        /// <summary>
        /// Gets the pure resolver task pool.
        /// </summary>
        ObjectPool<PureResolverTask> PureResolverTasks { get; }

        /// <summary>
        /// Gets the task buffer pool.
        /// </summary>
        ObjectPool<IExecutionTask?[]> TaskBuffers { get; }

        /// <summary>
        /// Resets the execution state.
        /// </summary>
        void Reset();
    }
}
