using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination.Extensions
{
    /// <summary>
    /// Provides cursor paging extensions to <see cref="IQueryable{T}"/>.
    /// </summary>
    public static class CursorPagingQueryableExtensions
    {
        /// <summary>
        /// Applies the cursor pagination algorithm to the <paramref name="query"/>.
        /// </summary>
        /// <param name="query">
        /// The query on which the the cursor pagination algorithm shall be applied to.
        /// </param>
        /// <param name="first">
        /// Returns the first _n_ elements from the list.
        /// </param>
        /// <param name="last">
        /// Returns the last _n_ elements from the list.
        /// </param>
        /// <param name="after">
        /// Returns the elements in the list that come after the specified cursor.
        /// </param>
        /// <param name="before">
        /// Returns the elements in the list that come before the specified cursor.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <typeparam name="TEntity">
        /// The entity type.
        /// </typeparam>
        /// <returns>
        /// Returns a connection instance that represents the result of applying the
        /// cursor paging algorithm to the provided <paramref name="query"/>.
        /// </returns>
        public static ValueTask<Connection<TEntity>> ApplyCursorPaginationAsync<TEntity>(
            this IQueryable<TEntity> query,
            int? first = null,
            int? last = null,
            string? after = null,
            string? before = null,
            CancellationToken cancellationToken = default)
            => ApplyCursorPaginationAsync(
                query,
                new CursorPagingArguments(first, last, after, before),
                cancellationToken);

        /// <summary>
        /// Applies the cursor pagination algorithm to the <paramref name="query"/>.
        /// </summary>
        /// <param name="query">
        /// The query on which the the cursor pagination algorithm shall be applied to.
        /// </param>
        /// <param name="arguments">
        /// The cursor paging arguments.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <typeparam name="TEntity">
        /// The entity type.
        /// </typeparam>
        /// <returns>
        /// Returns a connection instance that represents the result of applying the
        /// cursor paging algorithm to the provided <paramref name="query"/>.
        /// </returns>
        public static ValueTask<Connection<TEntity>> ApplyCursorPaginationAsync<TEntity>(
            this IQueryable<TEntity> query,
            CursorPagingArguments arguments,
            CancellationToken cancellationToken = default)
            => QueryableCursorPagination<TEntity>.Instance.ApplyPaginationAsync(
                query,
                arguments,
                cancellationToken);
    }
}
