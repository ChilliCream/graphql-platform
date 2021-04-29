using System;

namespace HotChocolate.Execution
{
    /// <summary>
    /// The execution task context can be used by an execution task to
    /// interact with the execution engine.
    /// </summary>
    public interface IExecutionTaskContext
    {
        /// <summary>
        /// Reports an error to the execution engine.
        /// </summary>
        /// <param name="task">
        /// The task that is reporting the error.
        /// </param>
        /// <param name="error">
        /// The error object.
        /// </param>
        void ReportError(IExecutionTask task, IError error);

        /// <summary>
        /// Reports an error to the execution engine.
        /// </summary>
        /// <param name="task">
        /// The task that is reporting the error.
        /// </param>
        /// <param name="exception">
        /// The exception that occured.
        /// </param>
        void ReportError(IExecutionTask task, Exception exception);

        /// <summary>
        /// Creates a diagnostic scope to track when the task is finished.
        /// </summary>
        /// <param name="task">
        /// The task that shall be tracked.
        /// </param>
        /// <returns>
        /// Returns a new disposable object that represents a scope.
        /// </returns>
        IDisposable Track(IExecutionTask task);

        /// <summary>
        /// Signals to the execution engine that this task has begun its work.
        /// </summary>
        void Started();

        /// <summary>
        /// Signals to the execution engine that this task has completed its work.
        /// </summary>
        void Completed();
    }
}
