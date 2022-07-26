using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.MongoDb.Paging;

internal sealed class MongoOffsetPagination<TEntity>
    : OffsetPaginationAlgorithm<IMongoPagingContainer<TEntity>, TEntity>
{
    protected override IMongoPagingContainer<TEntity> ApplySkip(
        IMongoPagingContainer<TEntity> query,
        int skip)
        => query.Skip(skip);

    protected override IMongoPagingContainer<TEntity> ApplyTake(
        IMongoPagingContainer<TEntity> query,
        int take)
        => query.Take(take);

    protected override async ValueTask<int> CountAsync(
        IMongoPagingContainer<TEntity> query,
        CancellationToken cancellationToken)
        => await query.CountAsync(cancellationToken).ConfigureAwait(false);

    protected override async ValueTask<IReadOnlyList<TEntity>> ExecuteAsync(
        IMongoPagingContainer<TEntity> query,
        CancellationToken cancellationToken)
    {
        var result = await query.ToListAsync(cancellationToken);
        return result;
    }
}
