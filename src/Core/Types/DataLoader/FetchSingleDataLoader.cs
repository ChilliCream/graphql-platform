using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.DataLoader
{
    internal sealed class FetchSingleDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue>
    {
        private readonly FetchSingle<TKey, TValue> _fetch;

        public FetchSingleDataLoader(FetchSingle<TKey, TValue> fetch)
            : this(fetch, 100)
        {
        }

        public FetchSingleDataLoader(
            FetchSingle<TKey, TValue> fetch,
            int cacheSize)
            : base(new DataLoaderOptions<TKey>
            {
                AutoDispatching = false,
                Batching = false,
                CacheSize = cacheSize,
                MaxBatchSize = 0,
                SlidingExpiration = TimeSpan.Zero
            })
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override async Task<IReadOnlyList<IResult<TValue>>> Fetch(
            IReadOnlyList<TKey> keys)
        {
            var items = new IResult<TValue>[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                try
                {
                    TValue value = await _fetch(keys[i]);
                    items[i] = Result<TValue>.Resolve(value);
                }
                catch (Exception ex)
                {
                    items[i] = Result<TValue>.Reject(ex);
                }
            }

            return items;
        }
    }
}
