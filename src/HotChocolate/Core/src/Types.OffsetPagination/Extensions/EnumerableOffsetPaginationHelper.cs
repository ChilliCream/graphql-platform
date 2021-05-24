using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types
{
    public static class EnumerableOffsetPagingExtensions
    {
        public static ValueTask<CollectionSegment> ApplyOffsetPaginationAsync<TItemType>(
            this IEnumerable<TItemType> source,
            int? skip = null,
            int? take = null,
            CancellationToken cancellationToken = default) =>
            OffsetPagingHelper.ApplyPagination(
                source,
                new OffsetPagingArguments(skip, take),
                (x, s) => x.Skip(s),
                (x, t) => x.Take(t),
                OffsetPagingHelper.ExecuteEnumerable,
                CountAsync,
                cancellationToken);

        public static ValueTask<CollectionSegment> ApplyOffsetPaginationAsync<TItemType>(
            this IEnumerable<TItemType> source,
            OffsetPagingArguments arguments,
            CancellationToken cancellationToken = default) =>
            OffsetPagingHelper.ApplyPagination(
                source,
                arguments,
                (x, skip) => x.Skip(skip),
                (x, take) => x.Take(take),
                OffsetPagingHelper.ExecuteEnumerable,
                CountAsync,
                cancellationToken);

        private static async ValueTask<int> CountAsync<TEntity>(
            IEnumerable<TEntity> source,
            CancellationToken cancellationToken) =>
            await Task.Run(source.Count, cancellationToken).ConfigureAwait(false);
    }
}
