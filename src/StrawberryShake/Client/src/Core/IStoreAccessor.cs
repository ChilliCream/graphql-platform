namespace StrawberryShake
{
    /// <summary>
    /// The store accessor allows access to the stores.
    /// </summary>
    public interface IStoreAccessor
    {
        /// <summary>
        /// Gets the operation store tracks and stores results by requests.
        /// </summary>
        IOperationStore OperationStore { get; }

        /// <summary>
        /// Get the entity store tracks and stores the GraphQL entities.
        /// </summary>
        IEntityStore EntityStore { get; }
    }
}
