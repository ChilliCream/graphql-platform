using System;

namespace GreenDonut
{
    /// <summary>
    /// An options object to configure the behavior for <c>DataLoader</c>.
    /// </summary>
    /// <typeparam name="TKey">A key type.</typeparam>
    public class DataLoaderOptions<TKey>
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="DataLoaderOptions{TKey}"/> class.
        /// </summary>
        public DataLoaderOptions()
        {
            AutoDispatching = false;
            Batching = true;
            BatchRequestDelay = Defaults.BatchRequestDelay;
            CacheSize = Defaults.CacheSize;
            Caching = true;
            SlidingExpiration = Defaults.SlidingExpiration;
        }

        /// <summary>
        /// Gets or sets a value indicating whether auto dispatching is
        /// enabled. The default value is <c>false</c>.
        /// </summary>
        public bool AutoDispatching { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether batching is enabled. The
        /// default value is <c>true</c>.
        /// </summary>
        public bool Batching { get; set; }

        /// <summary>
        /// Gets or sets the time period to wait before trying to setup another
        /// batch request. This property takes only effect if
        /// <see cref="Batching"/> is set to <c>true</c>. The default value is
        /// set to <c>50</c> milliseconds.
        /// </summary>
        public TimeSpan BatchRequestDelay { get; set; }

        /// <summary>
        /// Gets or sets a delegate which resolves the cache key which might
        /// differ from the key that is used to accessing the backend.
        /// The default value is set to <c>null</c>.
        /// </summary>
        public CacheKeyResolverDelegate<TKey> CacheKeyResolver { get; set; }

        /// <summary>
        /// Gets or sets the cache size. If set to <c>10</c> for example, it
        /// says only <c>10</c> cache entries can live inside the cache. When
        /// adding an additional entry the least recently used entry will be
        /// removed. The default value is set to <c>1000</c>.
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

        /// <summary>
        /// Gets or sets the sliding cache expiration. If a cahce entry expires
        /// the entry will be removed from the cache. If set to
        /// <see cref="TimeSpan.Zero"/> the sliding expiration is disabled.
        /// This means an entry could live forever if the
        /// <see cref="CacheSize"/> is not exceeded. The default value is set
        /// to <see cref="TimeSpan.Zero"/>.
        /// </summary>
        public TimeSpan SlidingExpiration { get; set; }
    }
}
