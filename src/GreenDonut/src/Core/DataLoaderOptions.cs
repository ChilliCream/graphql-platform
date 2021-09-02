namespace GreenDonut
{
    /// <summary>
    /// An options object to configure the behavior for <c>DataLoader</c>.
    /// </summary>
    public class DataLoaderOptions
    {
        /// <summary>
        /// Gets or sets the maximum batch size per request. If set to
        /// <c>0</c>, the request will be not cut into smaller batches. The
        /// default value is set to <c>0</c>.
        /// </summary>
        public int MaxBatchSize { get; set; } = 1024;

        /// <summary>
        /// Gets or sets a cache instance to either share a cache instance
        /// across several dataloader or to provide a custom cache
        /// implementation. In case no cache instance is provided, the
        /// dataloader will use the default cache implementation.
        /// The default value is set to <c>null</c>.
        /// </summary>
        public ITaskCache? Cache { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether caching is enabled. The
        /// default value is <c>true</c>.
        /// </summary>
        public bool Caching { get; set; } = true;

        /// <summary>
        /// Gets or sets a delegate which resolves the cache key which might
        /// differ from the key that is used to accessing the backend.
        /// The default value is set to <c>null</c>.
        /// </summary>
        public CacheKeyFactoryDelegate CacheKeyFactory { get; set; } =
            (type, key) => new TaskCacheKey(type, key);

        public IDataLoaderDiagnosticEvents? DiagnosticEvents { get; set; }

        public DataLoaderOptions Copy()
        {
            return new()
            {
                MaxBatchSize = MaxBatchSize,
                Cache = Cache,
                Caching = Caching,
                CacheKeyFactory = CacheKeyFactory,
                DiagnosticEvents = DiagnosticEvents,
            };
        }
    }
}
