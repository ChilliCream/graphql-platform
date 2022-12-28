using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.ElasticSearch.Paging;

public interface IElasticSearchPagingContainer<TEntity>
{
    Task<int> CountAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteQueryAsync(
        int offset,
        CancellationToken cancellationToken);

    ValueTask<List<TEntity>> ToListAsync(CancellationToken cancellationToken);

    IElasticSearchPagingContainer<TEntity> Skip(int skip);

    IElasticSearchPagingContainer<TEntity> Take(int take);
}
