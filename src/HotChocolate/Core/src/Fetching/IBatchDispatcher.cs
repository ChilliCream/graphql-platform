using System;

namespace HotChocolate.Fetching
{
    /// <summary>
    /// Describes a batch dispatcher that needs to be triggered in order to dispatch all batches
    /// enqueued so far.
    /// </summary>
    public interface IBatchDispatcher
    {
        /// <summary>
        /// Get a value that indicates whether the batch dispatcher has tasks to dispatch.
        /// </summary>
        bool HasTasks { get; }

        /// <summary>
        /// An event which is triggered whenever a new task is enqueued.
        /// </summary>
        event EventHandler? TaskEnqueued;

        /// <summary>
        /// Dispatches all batches enqueued so far.
        /// </summary>
        void Dispatch();
    }
}
