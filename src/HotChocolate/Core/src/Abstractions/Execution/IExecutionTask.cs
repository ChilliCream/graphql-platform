using System.Threading;

namespace HotChocolate.Execution
{
    /// <summary>
    /// Represents a task that shall be executed by the execution engine.
    /// </summary>
    public interface IExecutionTask
    {
        /// <summary>
        /// Defines if this task has completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Defines if this task was canceled.
        /// </summary>
        bool IsCanceled { get; }

        /// <summary>
        /// Starts the execution of this task.
        /// </summary>
        void BeginExecute(CancellationToken cancellationToken);
    }
}
