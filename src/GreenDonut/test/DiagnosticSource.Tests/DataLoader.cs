using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GreenDonut
{
    public class DataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue>
        where TKey : notnull
    {
        private readonly FetchDataDelegate<TKey, TValue> _fetch;

        public DataLoader(IBatchScheduler batchScheduler, FetchDataDelegate<TKey, TValue> fetch)
            : base(batchScheduler, null)
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override Task<IReadOnlyList<Result<TValue>>> FetchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken)
        {
            return _fetch(keys, cancellationToken);
        }
    }
}
