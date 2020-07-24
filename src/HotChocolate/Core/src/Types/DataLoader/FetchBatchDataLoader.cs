using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.DataLoader
{
    internal sealed class FetchBatchDataLoader<TKey, TValue>
        : BatchDataLoader<TKey, TValue>
    {
        private readonly FetchBatch<TKey, TValue> _fetch;

        public FetchBatchDataLoader(IBatchScheduler batchScheduler, FetchBatch<TKey, TValue> fetch) 
            : base(batchScheduler)
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override Task<IReadOnlyDictionary<TKey, TValue>> LoadBatchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken) => _fetch(keys);
    }
}
