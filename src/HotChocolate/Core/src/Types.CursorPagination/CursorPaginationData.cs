using System.Collections.Immutable;

namespace HotChocolate.Types.Pagination;

public readonly struct CursorPaginationData<TEntity>(
    ImmutableArray<Edge<TEntity>> edges,
    int? totalCount)
{
    public ImmutableArray<Edge<TEntity>> Edges { get; } = edges;

    public int? TotalCount { get; } = totalCount;
}

public interface ICursorPaginationQueryExecutor<in TQuery, TEntity> where TQuery : notnull
{
    ValueTask<int> CountAsync(
        TQuery originalQuery,
        CancellationToken cancellationToken);

    ValueTask<CursorPaginationData<TEntity>> QueryAsync(
        TQuery slicedQuery,
        int offset,
        bool includeTotalCount,
        CancellationToken cancellationToken);
}
