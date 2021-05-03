namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// Represents a GraphQL subscription instance within the execution engine.
    /// </summary>
    public interface ISubscription
    {
        /// <summary>
        /// Gets the internal subscription ID.
        /// </summary>
        ulong Id { get; }

        /// <summary>
        /// The compiled subscription operation.
        /// </summary>
        IPreparedOperation Operation { get; }
    }
}
