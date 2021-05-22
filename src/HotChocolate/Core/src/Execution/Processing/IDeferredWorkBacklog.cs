using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// Represents a backlog for deferred work.
    /// </summary>
    internal interface IDeferredWorkBacklog
    {
        /// <summary>
        /// <c>true</c> if there are no more deferred tasks in the backlog.
        /// </summary>
        /// <value></value>
        bool HasWork { get; }

        /// <summary>
        /// Tries to take the next task from the backlog.
        /// </summary>
        bool TryTake([NotNullWhen(true)] out IDeferredExecutionTask? executionTask);

        /// <summary>
        /// Adds new work to the backlog.
        /// </summary>
        void Register(IDeferredExecutionTask executionTask);
    }
}
