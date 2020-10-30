using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data
{
    public class EntityFrameworkExecutable<T> : EnumerableExecutable<T>
    {
        public EntityFrameworkExecutable(IEnumerable<T> source) : base(source)
        {
        }

        protected override async ValueTask<object> ApplyToListAsync(
            IQueryable<T> result,
            CancellationToken cancellationToken) =>
            await result.ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        protected override async Task<object?> ApplySingleOrDefaultAsync(
            IQueryable<T> result,
            CancellationToken cancellationToken)
        {
            return await result.SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task<object?> ApplyFirstOrDefaultAsync(
            IQueryable<T> result,
            CancellationToken cancellationToken)
        {
            return await result.FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task<object> ApplyCursorPagingAsync(
            IQueryable<T> result,
            CursorPagingArguments cursorPagingArguments,
            CancellationToken cancellationToken)
        {
            var count = await result.CountAsync(cancellationToken)
                .ConfigureAwait(false);

            IQueryable<T> edges =
                QueryableCursorPagingHandler<T>.SliceSource(
                    result,
                    cursorPagingArguments,
                    out var offset);

            IReadOnlyList<IndexEdge<T>> selectedEdges =
                await QueryableCursorPagingHandler<T>
                    .ExecuteAsync(edges, offset, cancellationToken)
                    .ConfigureAwait(false);

            return QueryableCursorPagingHandler<T>.CreateConnection(selectedEdges, count);
        }

        protected override async Task<object> ApplyOffsetPagingAsync(
            IQueryable<T> result,
            OffsetPagingArguments offsetPagingArguments,
            bool offsetPagingIncludeTotalCount,
            CancellationToken cancellationToken)
        {
            IQueryable<T> slicedSource =
                QueryableOffsetPagingHandler<T>.SliceSource(
                    result,
                    offsetPagingArguments,
                    out IQueryable<T> original);

            List<T> items = await slicedSource.ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return QueryableOffsetPagingHandler<T>.CreateCollectionSegment(
                offsetPagingArguments,
                offsetPagingIncludeTotalCount,
                items,
                original);
        }
    }
}
