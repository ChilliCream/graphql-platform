using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.DataLoader
{
    internal sealed class FetchBatchDataLoader<TKey, TValue>
        : BatchDataLoader<TKey, TValue>
    {
        private readonly FetchBatch<TKey, TValue> _fetch;

        public FetchBatchDataLoader(FetchBatch<TKey, TValue> fetch)
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override Task<IReadOnlyDictionary<TKey, TValue>> LoadBatchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken) => _fetch(keys);
    }
}
