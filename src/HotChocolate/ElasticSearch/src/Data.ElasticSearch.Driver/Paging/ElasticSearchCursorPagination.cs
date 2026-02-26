using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.ElasticSearch.Paging;

internal sealed class ElasticSearchCursorPagination<TEntity>
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
}
