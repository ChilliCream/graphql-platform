using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types
{
    public static class EnumerableCursorPagingExtensions
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
                Execute,
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
                Execute,
                CountAsync,
                cancellationToken);

        private static async ValueTask<int> CountAsync<TEntity>(
            IQueryable<TEntity> source,
            CancellationToken cancellationToken) =>
            await Task.Run(source.Count, cancellationToken).ConfigureAwait(false);

        private static async ValueTask<IReadOnlyList<IndexEdge<TEntity>>> Execute<TEntity>(
            IQueryable<TEntity> queryable,
            int offset,
            CancellationToken cancellationToken)
        {
            var list = new List<IndexEdge<TEntity>>();

            if (queryable is IAsyncEnumerable<TEntity> enumerable)
            {
                var index = offset;
                await foreach (TEntity item in enumerable.WithCancellation(cancellationToken)
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
                            foreach (TEntity item in queryable)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    break;
                                }

                                list.Add(IndexEdge<TEntity>.Create(item, index++));
                            }
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return list;
        }

        public static ValueTask<Connection> ApplyCursorPaginationAsync<TSource>(
            this IEnumerable<TSource> source,
            int? first = null,
            int? last = null,
            string? after = null,
            string? before = null,
            CancellationToken cancellationToken = default) =>
            ApplyCursorPaginationAsync(
                source.AsQueryable(),
                first,
                last,
                after,
                before,
                cancellationToken);

        public static ValueTask<Connection> ApplyCursorPaginationAsync<TSource>(
            this IEnumerable<TSource> source,
            CursorPagingArguments arguments,
            CancellationToken cancellationToken = default) =>
            ApplyCursorPaginationAsync(source.AsQueryable(), arguments, cancellationToken);
    }
}
