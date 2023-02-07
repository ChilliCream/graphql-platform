using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Raven.Pagination;

internal sealed class RavenCursorPagination<TEntity>
    : CursorPaginationAlgorithm<RavenPagingContainer<TEntity>, TEntity>
{
    public static RavenCursorPagination<TEntity> Instance { get; } = new();

    protected override RavenPagingContainer<TEntity> ApplySkip(
        RavenPagingContainer<TEntity> query,
        int skip)
        => query.Skip(skip);

    protected override RavenPagingContainer<TEntity> ApplyTake(
        RavenPagingContainer<TEntity> query,
        int take)
        => query.Take(take);

    protected override async ValueTask<int> CountAsync(
        RavenPagingContainer<TEntity> query,
        CancellationToken cancellationToken)
        => await query.CountAsync(cancellationToken).ConfigureAwait(false);

    protected override ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteAsync(
        RavenPagingContainer<TEntity> query,
        int offset,
        CancellationToken cancellationToken)
        => query.ExecuteQueryAsync(offset, cancellationToken);
}
