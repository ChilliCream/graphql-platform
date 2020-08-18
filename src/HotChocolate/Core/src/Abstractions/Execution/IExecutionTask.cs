using System;

namespace HotChocolate.Execution
{
    /// <summary>
    /// Represents a task that shall be executed by the execution engine.
    /// </summary>
    public interface IExecutionTask
    {
        bool IsCompleted { get; }

        /// <summary>
        /// Starts the execution of this task.
        /// </summary>
        void BeginExecute();
    }

    public interface IExecutionTaskDefinition
    {
        IExecutionTask Create(IExecutionTaskContext context);
    }

    public interface IExecutionTaskContext
    {
        void ReportError(IExecutionTask task, IError error);

        void ReportError(IExecutionTask task, Exception exception);

        IDisposable Track(IExecutionTask task);

        void Started();

        void Completed();
    }
}
