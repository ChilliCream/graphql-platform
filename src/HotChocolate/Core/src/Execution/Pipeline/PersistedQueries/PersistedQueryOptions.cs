namespace HotChocolate.Execution.Pipeline
{
    /// <summary>
    /// Represents common options for persisted queries
    /// </summary>
    public class PersistedQueryOptions
        : IPersistedQueryOptions
    {
        /// <inheritdoc />
        public bool BlockUnknownQueries { get; set; } = false;
    }
}
