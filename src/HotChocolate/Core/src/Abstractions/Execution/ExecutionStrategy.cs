namespace HotChocolate.Execution
{
    /// <summary>
    /// Represents the execution strategies.
    /// </summary>
    public enum ExecutionStrategy
    {
        /// <summary>
        /// Defines that a task or execution step has to be executed serial.
        /// </summary>
        Serial,

        /// <summary>
        /// Defines that a task or execution step can be executed in parallel.
        /// </summary>
        Parallel
    }
}
