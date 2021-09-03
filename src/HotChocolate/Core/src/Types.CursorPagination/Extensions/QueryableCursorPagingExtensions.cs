using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types
{
    public static class QueryableCursorPagingExtensions
    {
        public static ValueTask<Connection> ApplyCursorPaginationAsync<TSource>(
            this IQueryable<TSource> source,
            int? first = null,
            int? last = null,
            string? after = null,
            string? before = null,
            CancellationToken cancellationToken = default) =>
            CursorPagingHelper.ApplyPagination(
                source,
                new CursorPagingArguments(first, last, after, before),
                (x, skip) => x.Skip(skip),
                (x, take) => x.Take(take),
                ExecuteQueryAsync,
                CountAsync,
                cancellationToken);

        public static ValueTask<Connection> ApplyCursorPaginationAsync<TSource>(
            this IQueryable<TSource> source,
            CursorPagingArguments arguments,
            CancellationToken cancellationToken = default) =>
            CursorPagingHelper.ApplyPagination(
                source,
                arguments,
                (x, skip) => x.Skip(skip),
                (x, take) => x.Take(take),
                ExecuteQueryAsync,
                CountAsync,
                cancellationToken);

        private static async ValueTask<int> CountAsync<T>(
            IQueryable<T> source,
            CancellationToken cancellationToken) =>
            await Task.Run(source.Count, cancellationToken).ConfigureAwait(false);

        private static async ValueTask<IReadOnlyList<IndexEdge<T>>> ExecuteQueryAsync<T>(
            IQueryable<T> queryable,
            int offset,
            CancellationToken cancellationToken)
        {
            var list = new List<IndexEdge<T>>();

            if (queryable is IAsyncEnumerable<T> enumerable)
            {
                var index = offset;
                await foreach (T item in enumerable.WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    list.Add(IndexEdge<T>.Create(item, index++));
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    var index = offset;
                    foreach (T item in queryable)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        list.Add(IndexEdge<T>.Create(item, index++));
                    }
                },
                cancellationToken).ConfigureAwait(false);
            }

            return list;
        }
    }
}
