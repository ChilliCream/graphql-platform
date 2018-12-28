using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.DataLoader
{
    internal sealed class FetchDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue>
    {
        private readonly Fetch<TKey, TValue> _fetch;

        public FetchDataLoader(Fetch<TKey, TValue> fetch)
            : base(new DataLoaderOptions<TKey>
            {
                AutoDispatching = false,
                Batching = true,
                CacheSize = 100,
                MaxBatchSize = 0,
                SlidingExpiration = TimeSpan.Zero
            })
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override async Task<IReadOnlyList<IResult<TValue>>> Fetch(
            IReadOnlyList<TKey> keys)
        {
            IReadOnlyDictionary<TKey, TValue> result = await _fetch(keys);
            var items = new IResult<TValue>[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                if (result.TryGetValue(keys[i], out TValue value))
                {
                    items[i] = Result<TValue>.Resolve(value);
                }
            }

            return items;
        }
    }
}
