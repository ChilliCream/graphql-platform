namespace GreenDonut
{
    /// <summary>
    /// An options object to configure the behavior for <c>DataLoader</c>.
    /// </summary>
    /// <typeparam name="TKey">A key type.</typeparam>
    public class DataLoaderOptions<TKey>
        where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="DataLoaderOptions{TKey}"/> class.
        /// </summary>
        public DataLoaderOptions()
        {
            Batch = true;
            CacheSize = Defaults.CacheSize;
            Caching = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether requests should be sliced into smaller batches.
        /// The default value is <c>true</c>.
        /// </summary>
        public bool Batch { get; set; }

        /// <summary>
        /// Gets or sets a cache instance to either share a cache instance
        /// across several dataloader or to provide a custom cache
        /// implementation. In case no cache instance is provided, the
        /// dataloader will use the default cache implementation.
        /// The default value is set to <c>null</c>.
        /// </summary>
        public ITaskCache? Cache {get; set;}

        /// <summary>
        /// Gets or sets a delegate which resolves the cache key which might
        /// differ from the key that is used to accessing the backend.
        /// The default value is set to <c>null</c>.
        /// </summary>
        public CacheKeyResolverDelegate<TKey>? CacheKeyResolver { get; set; }

        /// <summary>
        /// Gets or sets the cache size. If set to <c>10</c> for example, it
        /// says only <c>10</c> cache entries can live inside the cache. When
        /// adding an additional entry the least recently used entry will be
        /// removed. The default value is set to <c>100</c>.
        /// </summary>
        public int CacheSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether caching is enabled. The
        /// default value is <c>true</c>.
        /// </summary>
        public bool Caching { get; set; }

        /// <summary>
        /// Gets or sets the maximum batch size per request. If set to
        /// <c>0</c>, the request will be not cut into smaller batches. The
        /// default value is set to <c>0</c>.
        /// </summary>
        public int MaxBatchSize { get; set; }
    }
}
