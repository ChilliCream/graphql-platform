using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.DataLoader
{
    internal sealed class FetchGroupedDataLoader<TKey, TValue>
        : GroupedDataLoader<TKey, TValue>
    {
        private readonly FetchGroup<TKey, TValue> _fetch;

        public FetchGroupedDataLoader(FetchGroup<TKey, TValue> fetch)
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override Task<ILookup<TKey, TValue>> LoadGroupedBatchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken)
        {
            return _fetch(keys);
        }
    }
}
