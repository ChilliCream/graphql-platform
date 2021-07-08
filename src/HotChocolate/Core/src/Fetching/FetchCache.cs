using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Fetching
{
    public delegate Task<TValue> FetchCacheCt<in TKey, TValue>(
        TKey key,
        CancellationToken cancellationToken)
        where TKey : notnull;
}
