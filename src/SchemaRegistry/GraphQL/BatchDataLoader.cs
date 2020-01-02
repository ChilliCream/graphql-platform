using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate;

namespace MarshmallowPie.GraphQL
{
    public abstract class BatchDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue>
        where TKey : notnull
        where TValue : class
    {
        protected override async Task<IReadOnlyList<Result<TValue>>> FetchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<TKey, TValue> entities =
                await FetchBatchAsync(keys, cancellationToken).ConfigureAwait(false);

            var list = new Result<TValue>[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                list[i] = entities.TryGetValue(keys[i], out TValue? entity)
                    ? Result<TValue>.Resolve(entity)
                    : Result<TValue>.Reject(new GraphQLException("The specified id is invalid."));
            }
            return list;
        }

        protected abstract Task<IReadOnlyDictionary<TKey, TValue>> FetchBatchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken);
    }
}
