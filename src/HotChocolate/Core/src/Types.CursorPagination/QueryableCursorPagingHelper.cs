using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    internal sealed class QueryableCursorPagingHelper<TEntity>
        : CursorPagingHelper<IQueryable<TEntity>, TEntity>
    {
        protected override IQueryable<TEntity> ApplySkip(IQueryable<TEntity> source, int skip)
            => source.Skip(skip);

        protected override IQueryable<TEntity> ApplyTake(IQueryable<TEntity> source, int take)
            => source.Take(take);

        protected override async ValueTask<int> CountAsync(
            IQueryable<TEntity> source,
            CancellationToken cancellationToken)
            => await Task.Run(source.Count, cancellationToken).ConfigureAwait(false);

        protected override async ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteAsync(
            IQueryable<TEntity> source,
            int offset,
            CancellationToken cancellationToken)
        {
            var list = new List<IndexEdge<TEntity>>();

            if (source is IAsyncEnumerable<TEntity> enumerable)
            {
                var index = offset;
                await foreach (TEntity item in enumerable
                    .WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    list.Add(IndexEdge<TEntity>.Create(item, index++));
                }
            }
            else
            {
                await Task.Run(() =>
                    {
                        var index = offset;
                        foreach (TEntity item in source)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            list.Add(IndexEdge<TEntity>.Create(item, index++));
                        }
                    },
                    cancellationToken).ConfigureAwait(false);
            }

            return list;
        }
    }
}
