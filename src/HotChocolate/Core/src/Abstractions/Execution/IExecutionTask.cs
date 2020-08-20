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
}
