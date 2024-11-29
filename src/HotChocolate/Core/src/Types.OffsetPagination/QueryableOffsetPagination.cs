namespace HotChocolate.Types.Pagination;

internal sealed class QueryableOffsetPagination<TEntity>
    : OffsetPaginationAlgorithm<IQueryable<TEntity>, TEntity>
{
    public static QueryableOffsetPagination<TEntity> Instance { get; } = new();

    protected override IQueryable<TEntity> ApplySkip(IQueryable<TEntity> query, int skip)
        => query.Skip(skip);

    protected override IQueryable<TEntity> ApplyTake(IQueryable<TEntity> query, int take)
        => query.Take(take);

    protected override async ValueTask<int> CountAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken)
        => await Task.Run(query.Count, cancellationToken).ConfigureAwait(false);

    protected override async ValueTask<IReadOnlyList<TEntity>> ExecuteAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken)
    {
        var list = new List<TEntity>();

        if (query is IAsyncEnumerable<TEntity> enumerable)
        {
            await foreach (var item in enumerable.WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                list.Add(item);
            }
        }
        else
        {
            await Task.Run(() =>
                {
                    foreach (var item in query)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        list.Add(item);
                    }
                }, cancellationToken)
                .ConfigureAwait(false);
        }

        return list;
    }
}
