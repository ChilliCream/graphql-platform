using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.MongoDb.Paging;

internal sealed class MongoCursorPaginationAlgorithm<TEntity>
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
}
