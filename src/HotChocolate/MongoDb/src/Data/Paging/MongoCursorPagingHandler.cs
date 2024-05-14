using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.MongoDb.Paging;

internal sealed class MongoCursorPagination<TEntity>
    : CursorPaginationAlgorithm<IMongoPagingContainer<TEntity>, TEntity>
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

    protected override ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteAsync(
        IMongoPagingContainer<TEntity> query,
        int offset,
        CancellationToken cancellationToken)
        => query.ExecuteQueryAsync(offset, cancellationToken);
}
