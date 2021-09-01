using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace GreenDonut
{
    public abstract class GroupedDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue[]>
        where TKey : notnull
    {
        protected GroupedDataLoader(
            IBatchScheduler batchScheduler,
            DataLoaderOptions? options = null)
            : base(batchScheduler, options)
        { }

        protected sealed override async ValueTask FetchAsync(
            IReadOnlyList<TKey> keys,
            Memory<Result<TValue[]>> results,
            CancellationToken cancellationToken)
        {
            ILookup<TKey, TValue> result =
                await LoadGroupedBatchAsync(keys, cancellationToken)
                    .ConfigureAwait(false);

            CopyResults(keys, results.Span, result);
        }

        private void CopyResults(
            IReadOnlyList<TKey> keys,
            Span<Result<TValue[]>> results,
            ILookup<TKey, TValue> resultLookup)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                results[i] = resultLookup[keys[i]].ToArray();
            }
        }

        protected abstract Task<ILookup<TKey, TValue>> LoadGroupedBatchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken);
    }
}
