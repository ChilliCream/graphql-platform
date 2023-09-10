using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Raven.Pagination;

internal sealed class RavenOffsetPagination<TEntity>
    : OffsetPaginationAlgorithm<RavenPagingContainer<TEntity>, TEntity>
{
    public static RavenOffsetPagination<TEntity> Instance { get; } = new();

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

    protected override async ValueTask<IReadOnlyList<TEntity>> ExecuteAsync(
        RavenPagingContainer<TEntity> query,
        CancellationToken cancellationToken)
        => await query.ToListAsync(cancellationToken);
}
