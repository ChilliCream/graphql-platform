using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.DataLoader
{
    internal sealed class FetchSingleDataLoader<TKey, TValue>
        : CacheDataLoader<TKey, TValue>
    {
        private readonly FetchCache<TKey, TValue> _fetch;

        public FetchSingleDataLoader(FetchCache<TKey, TValue> fetch)
            : this(fetch, DataLoaderDefaults.CacheSize)
        {
        }

        public FetchSingleDataLoader(FetchCache<TKey, TValue> fetch, int cacheSize)
            : base(cacheSize)
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override Task<TValue> LoadSingleAsync(
            TKey key,
            CancellationToken cancellationToken) =>
            _fetch(key);
    }
}
