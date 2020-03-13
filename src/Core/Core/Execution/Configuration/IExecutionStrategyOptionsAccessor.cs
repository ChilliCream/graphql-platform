namespace HotChocolate.Execution.Configuration
{
    /// <summary>
    /// Represents a dedicated options accessor to read the execution strategy options
    /// </summary>
    public interface IExecutionStrategyOptionsAccessor
    {
        /// <summary>
        /// Defines that the query graph shall be traversed and execution serially.
        /// </summary>
        bool? ForceSerialExecution { get; }
    }
}
