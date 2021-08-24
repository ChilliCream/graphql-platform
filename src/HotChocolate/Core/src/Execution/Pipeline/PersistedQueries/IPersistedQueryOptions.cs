namespace HotChocolate.Execution.Pipeline
{
    /// <summary>
    /// Represents common options for persisted queries
    /// </summary>
    public interface IPersistedQueryOptions
    {
        /// <summary>
        /// If true, only stored persisted queries can be executed on the server
        /// </summary>
        public bool BlockUnknownQueries { get; }
    }
}
