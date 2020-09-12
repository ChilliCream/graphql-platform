namespace HotChocolate.Execution.Options
{
    /// <summary>
    /// Represents a dedicated options accessor to read the configured query
    /// cache size.
    /// </summary>
    public interface IDocumentCacheSizeOptionsAccessor
    {
        /// <summary>
        /// Gets maximum amount of queries that can be cached. The default
        /// value is <c>100</c>. The minimum allowed value is <c>10</c>.
        /// </summary>
        int QueryCacheSize { get; }
    }
}
