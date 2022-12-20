using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination;

internal sealed class QueryableCursorPagination<TEntity>
    : CursorPaginationAlgorithm<IQueryable<TEntity>, TEntity>
{
    public static QueryableCursorPagination<TEntity> Instance { get; } = new();

    protected override IQueryable<TEntity> ApplySkip(IQueryable<TEntity> query, int skip)
        => query.Skip(skip);

    protected override IQueryable<TEntity> ApplyTake(IQueryable<TEntity> query, int take)
        => query.Take(take);

    protected override async ValueTask<int> CountAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken)
        => await Task.Run(query.Count, cancellationToken).ConfigureAwait(false);

    protected override async ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteAsync(
        IQueryable<TEntity> query,
        int offset,
        CancellationToken cancellationToken)
    {
        var list = new List<IndexEdge<TEntity>>();

        if (query is IAsyncEnumerable<TEntity> enumerable)
        {
            var index = offset;
            await foreach (var item in enumerable
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
                    foreach (var item in query)
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
