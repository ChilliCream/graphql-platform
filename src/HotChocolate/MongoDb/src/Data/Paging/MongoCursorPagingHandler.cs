using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.MongoDb.Paging
{
    internal sealed class MongoCursorPagingHandler<TEntity>
        : CursorPagingHelper<IMongoPagingContainer<TEntity>, TEntity>
    {
        protected override IMongoPagingContainer<TEntity> ApplySkip(
            IMongoPagingContainer<TEntity> source,
            int skip)
            => source.Skip(skip);

        protected override IMongoPagingContainer<TEntity> ApplyTake(
            IMongoPagingContainer<TEntity> source,
            int take)
            => source.Take(take);

        protected override async ValueTask<int> CountAsync(
            IMongoPagingContainer<TEntity> source,
            CancellationToken cancellationToken)
            => await source.CountAsync(cancellationToken).ConfigureAwait(false);

        protected override ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteAsync(
            IMongoPagingContainer<TEntity> source,
            int offset,
            CancellationToken cancellationToken)
            => source.ExecuteQueryAsync(offset, cancellationToken);
    }
}
