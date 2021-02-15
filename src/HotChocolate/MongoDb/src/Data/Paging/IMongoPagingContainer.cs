using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.MongoDb.Paging
{
    internal interface IMongoPagingContainer<TEntity>
    {
        Task<int> CountAsync(CancellationToken cancellationToken);

        ValueTask<IReadOnlyList<IndexEdge<TEntity>>> ToIndexEdgesAsync(
            int offset,
            CancellationToken cancellationToken);

        ValueTask<List<TEntity>> ToListAsync(CancellationToken cancellationToken);

        IMongoPagingContainer<TEntity> Skip(int skip);

        IMongoPagingContainer<TEntity> Take(int take);
    }
}
