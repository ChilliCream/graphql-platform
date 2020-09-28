using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

#nullable enable

namespace HotChocolate.DataLoader
{
    public abstract class GroupedDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue[]>
        where TKey : notnull
    {
        protected GroupedDataLoader(
            IBatchScheduler batchScheduler,
            DataLoaderOptions<TKey>? options = null)
            : base(batchScheduler, options)
        { }

        protected sealed override async ValueTask<IReadOnlyList<Result<TValue[]>>> FetchAsync(
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
