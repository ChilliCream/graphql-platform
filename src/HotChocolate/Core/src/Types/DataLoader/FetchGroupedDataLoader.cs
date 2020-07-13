using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.DataLoader
{
    internal sealed class FetchGroupedDataLoader<TKey, TValue>
        : GroupedDataLoader<TKey, TValue>
    {
        private readonly FetchGroup<TKey, TValue> _fetch;

        public FetchGroupedDataLoader(
            IBatchScheduler batchScheduler,
            FetchGroup<TKey, TValue> fetch) 
            : base(batchScheduler)
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
