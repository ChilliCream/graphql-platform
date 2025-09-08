using System.Collections.Immutable;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.MongoDb.Paging;

internal interface IMongoPagingContainer<TEntity>
{
    Task<int> CountAsync(CancellationToken cancellationToken);

    Task<ImmutableArray<Edge<TEntity>>> QueryAsync(
        int offset,
        CancellationToken cancellationToken);

    Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken);

    IMongoPagingContainer<TEntity> Skip(int skip);

    IMongoPagingContainer<TEntity> Take(int take);
}
