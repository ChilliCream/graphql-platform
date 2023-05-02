using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.ElasticSearch.Paging;

public class ElasticSearchCursorPagination<TEntity>
    : CursorPaginationAlgorithm<IElasticSearchExecutable<TEntity>, TEntity>
{
    /// <inheritdoc />
    protected override IElasticSearchExecutable<TEntity> ApplySkip(
        IElasticSearchExecutable<TEntity> query,
        int skip)
        => query.WithSkip(skip);

    /// <inheritdoc />
    protected override IElasticSearchExecutable<TEntity> ApplyTake(
        IElasticSearchExecutable<TEntity> query,
        int take)
        => query.WithTake(take);

    /// <inheritdoc />
    protected override async ValueTask<int> CountAsync(
        IElasticSearchExecutable<TEntity> query,
        CancellationToken cancellationToken)
        => await query.CountAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    protected override async ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteAsync(
        IElasticSearchExecutable<TEntity> query,
        int offset,
        CancellationToken cancellationToken)
    {
        query.WithSkip(offset);
        var results = await query.ExecuteAsync(cancellationToken);
        return results
            .Select((searchResult, i) => IndexEdge<TEntity>.Create(searchResult, offset + i))
            .ToList();
    }

}
