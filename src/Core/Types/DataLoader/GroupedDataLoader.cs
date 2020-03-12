using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.DataLoader
{
    public abstract class GroupedDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue[]>
    {
        private static DataLoaderOptions<TKey> _options = new DataLoaderOptions<TKey>
        {
            AutoDispatching = false,
            Batching = true,
            CacheSize = DataLoaderDefaults.CacheSize,
            MaxBatchSize = DataLoaderDefaults.MaxBatchSize,
            SlidingExpiration = TimeSpan.Zero
        };

        protected GroupedDataLoader()
            : base(_options)
        {
        }

        protected sealed override async Task<IReadOnlyList<Result<TValue[]>>> FetchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken)
        {
            ILookup<TKey, TValue> result =
                await LoadGroupedBatchAsync(keys, cancellationToken)
                    .ConfigureAwait(false);

            var items = new Result<TValue[]>[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                items[i] = result[keys[i]].ToArray();
            }

            return items;
        }

        protected abstract Task<ILookup<TKey, TValue>> LoadGroupedBatchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken);
    }
}
