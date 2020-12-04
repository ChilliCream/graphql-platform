using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Paging
{
    internal class FindFluentPagingContainer<TEntity> : IMongoPagingContainer<TEntity>
    {
        public readonly IFindFluent<TEntity, TEntity> _source;

        public FindFluentPagingContainer(IFindFluent<TEntity, TEntity> source)
        {
            _source = source;
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken)
        {
            return (int)await _source
                .CountDocumentsAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async ValueTask<IReadOnlyList<IndexEdge<TEntity>>> ToIndexEdgesAsync(
            int offset,
            CancellationToken cancellationToken)
        {
            var list = new List<IndexEdge<TEntity>>();

            using IAsyncCursor<TEntity> cursor = await _source
                .ToCursorAsync(cancellationToken)
                .ConfigureAwait(false);

            var index = offset;
            while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (TEntity item in cursor.Current)
                {
                    list.Add(IndexEdge<TEntity>.Create(item, index++));
                }
            }

            return list;
        }

        public async ValueTask<List<TEntity>> ToListAsync(CancellationToken cancellationToken)
        {
            return await _source.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public IMongoPagingContainer<TEntity> Skip(int skip)
        {
            return new FindFluentPagingContainer<TEntity>(_source.Skip(skip));
        }

        public IMongoPagingContainer<TEntity> Take(int take)
        {
            return new FindFluentPagingContainer<TEntity>(_source.Limit(take));
        }

        public static FindFluentPagingContainer<TEntity> New(
            IFindFluent<TEntity, TEntity> find) =>
            new FindFluentPagingContainer<TEntity>(find);
    }
}
