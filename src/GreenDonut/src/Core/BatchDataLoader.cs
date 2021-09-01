using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace GreenDonut
{
    public abstract class BatchDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue>
        where TKey : notnull
    {
        protected BatchDataLoader(
            IBatchScheduler batchScheduler,
            DataLoaderOptions? options = null)
            : base(batchScheduler, options)
        { }

        protected sealed override async ValueTask FetchAsync(
            IReadOnlyList<TKey> keys,
            Memory<Result<TValue>> results,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<TKey, TValue> result =
                await LoadBatchAsync(keys, cancellationToken)
                    .ConfigureAwait(false);

            CopyResults(keys, results.Span, result);
        }

        private void CopyResults(
            IReadOnlyList<TKey> keys,
            Span<Result<TValue>> results,
            IReadOnlyDictionary<TKey, TValue> resultMap)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                if (resultMap.TryGetValue(keys[i], out TValue? value))
                {
                    results[i] = value;
                }
            }
        }

        protected abstract Task<IReadOnlyDictionary<TKey, TValue>> LoadBatchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken);
    }
}
