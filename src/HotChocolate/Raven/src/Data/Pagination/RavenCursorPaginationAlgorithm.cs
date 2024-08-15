using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Raven.Pagination;

internal sealed class RavenCursorPaginationAlgorithm<TEntity>
    : CursorPaginationAlgorithm<RavenPagingContainer<TEntity>, TEntity>
{
    public static RavenCursorPaginationAlgorithm<TEntity> Instance { get; } = new();

    protected override RavenPagingContainer<TEntity> ApplySkip(
        RavenPagingContainer<TEntity> query,
        int skip)
        => query.Skip(skip);

    protected override RavenPagingContainer<TEntity> ApplyTake(
        RavenPagingContainer<TEntity> query,
        int take)
        => query.Take(take);
}
