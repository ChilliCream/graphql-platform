using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.DataLoader
{
    public delegate Task<TValue> FetchCacheCt<TKey, TValue>(
        TKey key,
        CancellationToken cancellationToken)
        where TKey : notnull;
}
