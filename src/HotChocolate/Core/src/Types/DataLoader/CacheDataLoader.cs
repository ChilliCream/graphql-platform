using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Fetching;

namespace HotChocolate.DataLoader
{
    public abstract class CacheDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue>
    {
        protected CacheDataLoader(IAutoBatchDispatcher batchScheduler, FetchCache<TKey, TValue> fetch)
            : base(batchScheduler)
        { }

        protected CacheDataLoader(IAutoBatchDispatcher batchScheduler, int cacheSize)
            : base(batchScheduler, new DataLoaderOptions<TKey> { CacheSize = cacheSize })
        { }

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
    }
}
