using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.DataLoader
{
    internal sealed class FetchCacheDataLoader<TKey, TValue>
        : CacheDataLoader<TKey, TValue>
        where TKey : notnull
    {
        private readonly FetchCacheCt<TKey, TValue> _fetch;

        public FetchCacheDataLoader(
            FetchCacheCt<TKey, TValue> fetch, 
            int cacheSize)
            : base(cacheSize)
        {
            _fetch = fetch;
        }

        protected override Task<TValue> LoadSingleAsync(
            TKey key,
            CancellationToken cancellationToken) =>
            _fetch(key, cancellationToken);
    }
}
