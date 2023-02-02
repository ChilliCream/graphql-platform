using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace GreenDonut;

public class DataLoader<TKey, TValue> : DataLoaderBase<TKey, TValue> where TKey : notnull
{
    private readonly FetchDataDelegate<TKey, TValue> _fetch;

    public DataLoader(
        FetchDataDelegate<TKey, TValue> fetch,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
    }

    protected override ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue>> results,
        CancellationToken cancellationToken)
        => _fetch(keys, results, cancellationToken);
}