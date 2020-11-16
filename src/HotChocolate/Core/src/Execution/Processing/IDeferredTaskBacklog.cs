using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// Represents a backlog for deferred work.
    /// </summary>
    internal interface IDeferredTaskBacklog
    {
        /// <summary>
        /// <c>true</c> if there are no more deferred tasks in the backlog.
        /// </summary>
        /// <value></value>
        bool IsEmpty { get; }

        /// <summary>
        /// Tries to take the next task from the backlog.
        /// </summary>
        bool TryTake([NotNullWhen(true)] out IDeferredExecutionTask? task);

        /// <summary>
        /// Adds new work to the backlog.
        /// </summary>
        void Register(IDeferredExecutionTask deferredTask);

        /// <summary>
        /// Clears the backlog.
        /// </summary>
        void Clear();
    }
}
