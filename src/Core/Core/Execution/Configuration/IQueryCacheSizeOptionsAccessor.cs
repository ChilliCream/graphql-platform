namespace HotChocolate.Execution.Configuration
{
    public interface IQueryCacheSizeOptionsAccessor
    {
        /// <summary>
        /// Gets the amount of items that the cache can hold.
        /// The minimum allowed cache site is ten items.
        /// </summary>
        /// <value>
        /// The query cache size.
        /// </value>
        int QueryCacheSize { get; }
    }
}
