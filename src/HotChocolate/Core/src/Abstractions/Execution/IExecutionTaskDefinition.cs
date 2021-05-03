namespace HotChocolate.Execution
{
    /// <summary>
    /// The execution task definition represents one kind of execution task.
    /// </summary>
    public interface IExecutionTaskDefinition
    {
        /// <summary>
        /// Creates a new execution task from this definition.
        /// </summary>
        /// <param name="context">
        /// The execution task context.
        /// </param>
        /// <returns>
        /// Returns a new execution task.
        /// </returns>
        IExecutionTask Create(IExecutionTaskContext context);
    }
}
