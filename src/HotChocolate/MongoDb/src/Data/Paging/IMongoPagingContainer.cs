using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.MongoDb.Paging;

internal interface IMongoPagingContainer<TEntity>
{
    Task<int> CountAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteQueryAsync(
        int offset,
        CancellationToken cancellationToken);

    ValueTask<List<TEntity>> ToListAsync(CancellationToken cancellationToken);

    IMongoPagingContainer<TEntity> Skip(int skip);

    IMongoPagingContainer<TEntity> Take(int take);
}
