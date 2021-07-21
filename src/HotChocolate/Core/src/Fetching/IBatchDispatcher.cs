using System;
using HotChocolate.Execution;

namespace HotChocolate.Fetching
{
    /// <summary>
    /// The execution engine batch dispatcher.
    /// </summary>
    public interface IBatchDispatcher
    {
        /// <summary>
        /// Signals that a batch task was enqueued.
        /// </summary>
        event EventHandler? TaskEnqueued;

        /// <summary>
        /// Defines that this instance has batch tasks that can be dispatched.
        /// </summary>
        bool HasTasks { get; }

        /// <summary>
        /// Defines if the batch dispatcher shall dispatch tasks directly when they are enqueued.
        /// </summary>
        bool DispatchOnSchedule { get; set; }

        /// <summary>
        /// Initializes the batch dispatcher with the current task execution context,
        /// </summary>
        /// <param name="context">
        /// The task execution context which allows to enqueue new tasks.
        /// </param>
        void Initialize(IExecutionTaskContext context);

        /// <summary>
        /// Dispatches execution tasks to the execution engine work backlog.
        /// </summary>
        void Dispatch();
    }
}
