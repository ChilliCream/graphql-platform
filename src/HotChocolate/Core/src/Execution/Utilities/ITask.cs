namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    /// Represents a task that shall be executed by the execution engine.
    /// </summary>
    internal interface ITask
    {
        /// <summary>
        /// Starts the execution of this task.
        /// </summary>
        void BeginExecute();
    }
}
