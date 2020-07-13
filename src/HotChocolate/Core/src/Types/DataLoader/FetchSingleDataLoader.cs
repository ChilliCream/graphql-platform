using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;

namespace HotChocolate.DataLoader
{
    internal sealed class FetchSingleDataLoader<TKey, TValue>
        : CacheDataLoader<TKey, TValue>
    {
        private readonly FetchCache<TKey, TValue> _fetch;

        public FetchSingleDataLoader(
            IAutoBatchDispatcher batchScheduler,
            FetchCache<TKey, TValue> fetch)
            : this(batchScheduler, fetch, DataLoaderDefaults.CacheSize)
        {
        }

        public FetchSingleDataLoader(
            IAutoBatchDispatcher batchScheduler,
            FetchCache<TKey, TValue> fetch,
            int cacheSize)
            : base(batchScheduler, cacheSize)
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override Task<TValue> LoadSingleAsync(
            TKey key,
            CancellationToken cancellationToken) =>
            _fetch(key);
    }
}
