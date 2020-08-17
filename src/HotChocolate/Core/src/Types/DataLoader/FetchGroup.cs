using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

#nullable enable

namespace HotChocolate.DataLoader
{
    public delegate Task<ILookup<TKey, TValue>> FetchGroup<TKey, TValue>(
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken)
        where TKey : notnull;
}
