namespace HotChocolate.Types.Pagination;

internal sealed class QueryableCursorPaginationAlgorithm<TEntity>
    : CursorPaginationAlgorithm<IQueryable<TEntity>, TEntity>
{
    public static QueryableCursorPaginationAlgorithm<TEntity> Instance { get; } = new();

    protected override IQueryable<TEntity> ApplySkip(IQueryable<TEntity> query, int skip)
        => query.Skip(skip);

    protected override IQueryable<TEntity> ApplyTake(IQueryable<TEntity> query, int take)
        => query.Take(take);
}
