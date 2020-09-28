using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.DataLoader
{
    public delegate Task<IReadOnlyDictionary<TKey, TValue>> FetchBatch<TKey, TValue>(
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken)
        where TKey : notnull;
}
