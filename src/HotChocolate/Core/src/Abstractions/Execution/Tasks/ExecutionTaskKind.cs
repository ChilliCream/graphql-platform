namespace HotChocolate.Execution
{
    /// <summary>
    /// The execution task kind defines the task behavior during execution.
    /// </summary>
    public enum ExecutionTaskKind
    {
        /// <summary>
        /// Tasks that can be executed in parallel.
        /// </summary>
        Parallel,

        /// <summary>
        /// Tasks that need to be executed serially.
        /// </summary>
        Serial,

        /// <summary>
        /// Tasks that have no side-effects and are synchronous.
        /// </summary>
        Pure
    }
}
