using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.DataLoader
{
    public abstract class CacheDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue>
    {
        private static readonly DataLoaderOptions<TKey> _defaultOptions =
            CreateOptions(DataLoaderDefaults.CacheSize);

        protected CacheDataLoader(FetchCache<TKey, TValue> fetch)
            : base(_defaultOptions)
        {
        }

        protected CacheDataLoader(int cacheSize)
            : base(CreateOptions(cacheSize))
        {
        }

        protected sealed override async Task<IReadOnlyList<Result<TValue>>> FetchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken)
        {
            var items = new Result<TValue>[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                try
                {
                    items[i] = await LoadSingleAsync(keys[i], cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    items[i] = ex;
                }
            }

            return items;
        }

        protected abstract Task<TValue> LoadSingleAsync(TKey key, CancellationToken cancellationToken);

        private static DataLoaderOptions<TKey> CreateOptions(int cacheSize) =>
            new DataLoaderOptions<TKey>
            {
                AutoDispatching = false,
                Batching = false,
                CacheSize = cacheSize,
                MaxBatchSize = DataLoaderDefaults.MaxBatchSize,
                SlidingExpiration = TimeSpan.Zero
            };
    }
}
