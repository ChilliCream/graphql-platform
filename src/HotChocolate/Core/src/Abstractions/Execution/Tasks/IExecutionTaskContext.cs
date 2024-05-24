using System;

namespace HotChocolate.Execution;

/// <summary>
/// The execution task context can be used by an execution task to
/// interact with the execution engine.
/// </summary>
public interface IExecutionTaskContext
{
    /// <summary>
    /// Tracks the running task for the diagnostics.
    /// </summary>
    /// <param name="task">The task that shall be tracked.</param>
    /// <returns>Returns a disposable representing the task activity scope.</returns>
    IDisposable Track(IExecutionTask task);

    /// <summary>
    /// Signals to the execution engine that the task has finished.
    /// </summary>
    /// <param name="task">The task that has been completed.</param>
    void Completed(IExecutionTask task);

    /// <summary>
    /// Reports an error that happened during the task execution.
    /// </summary>
    /// <param name="task">
    /// The task that is reporting the error.
    /// </param>
    /// <param name="error">
    /// The GraphQL error.
    /// </param>
    void ReportError(IExecutionTask task, IError error);

    /// <summary>
    /// Reports an error that happened during the task execution.
    /// </summary>
    /// <param name="task">
    /// The task that is reporting the error.
    /// </param>
    /// <param name="exception">
    /// The exception that happened during execution.
    /// </param>
    void ReportError(IExecutionTask task, Exception exception);

    /// <summary>
    /// Registers a new execution task.
    /// An execution task may register new execution tasks
    /// before it has hit <see cref="Completed"/>.
    /// </summary>
    /// <param name="task">
    /// The new execution task.
    /// </param>
    void Register(IExecutionTask task);
}
