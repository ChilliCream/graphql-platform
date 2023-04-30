using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.ElasticSearch.Paging;

public class ElasticSearchOffsetPagination<TEntity>
    : OffsetPaginationAlgorithm<IElasticSearchPagingContainer<TEntity>, TEntity>
{
    /// <inheritdoc />
    protected override IElasticSearchPagingContainer<TEntity> ApplySkip(
        IElasticSearchPagingContainer<TEntity> query,
        int skip)
        => query.Skip(skip);

    /// <inheritdoc />
    protected override IElasticSearchPagingContainer<TEntity> ApplyTake(
        IElasticSearchPagingContainer<TEntity> query,
        int take)
        => query.Take(take);

    /// <inheritdoc />
    protected override async ValueTask<int> CountAsync(
        IElasticSearchPagingContainer<TEntity> query,
        CancellationToken cancellationToken)
        => await query.CountAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    protected override async ValueTask<IReadOnlyList<TEntity>> ExecuteAsync(
        IElasticSearchPagingContainer<TEntity> query,
        CancellationToken cancellationToken)
    {
        var result = await query.ToListAsync(cancellationToken);
        return result;
    }
}
