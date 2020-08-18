using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Fetching;

#nullable enable

namespace HotChocolate.DataLoader
{
    public abstract class CacheDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue>
        where TKey : notnull
    {
        protected CacheDataLoader(int cacheSize)
            : base(
                AutoBatchScheduler.Default,
                new DataLoaderOptions<TKey> { CacheSize = cacheSize, MaxBatchSize = 1 })
        { }

        protected sealed override async ValueTask<IReadOnlyList<Result<TValue>>> FetchAsync(
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

        protected abstract Task<TValue> LoadSingleAsync(
            TKey key,
            CancellationToken cancellationToken);
    }
}
