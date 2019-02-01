using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.DataLoader
{
    internal sealed class FetchGroupedDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue[]>
    {
        private readonly FetchGrouped<TKey, TValue> _fetch;

        public FetchGroupedDataLoader(FetchGrouped<TKey, TValue> fetch)
            : base(new DataLoaderOptions<TKey>
            {
                AutoDispatching = false,
                Batching = true,
                CacheSize = DataLoaderDefaults.CacheSize,
                MaxBatchSize = DataLoaderDefaults.MaxBatchSize,
                SlidingExpiration = TimeSpan.Zero
            })
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override async Task<IReadOnlyList<Result<TValue[]>>>
            FetchAsync(
                IReadOnlyList<TKey> keys,
                CancellationToken cancellationToken)
        {
            ILookup<TKey, TValue> result = await _fetch(keys)
                .ConfigureAwait(false);

            var items = new Result<TValue[]>[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                items[i] = result[keys[i]].ToArray();
            }

            return items;
        }
    }
}
