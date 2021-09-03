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
            this IEnumerable<TSource> source,
            int? first = null,
            int? last = null,
            string? after = null,
            string? before = null,
            CancellationToken cancellationToken = default) =>
            source.AsQueryable()
                .ApplyCursorPaginationAsync(first, last, after, before, cancellationToken);

        public static ValueTask<Connection> ApplyCursorPaginationAsync<TSource>(
            this IEnumerable<TSource> source,
            CursorPagingArguments arguments,
            CancellationToken cancellationToken = default) =>
            source.AsQueryable()
                .ApplyCursorPaginationAsync(arguments, cancellationToken);
    }
}
