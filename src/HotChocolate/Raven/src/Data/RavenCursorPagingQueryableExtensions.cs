using HotChocolate.Data.Raven.Pagination;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace HotChocolate.Data;

/// <summary>
/// Provides cursor paging extensions to <see cref="IRavenQueryable{T}"/>.
/// </summary>
public static class RavenCursorPagingQueryableExtensions
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
        this IRavenQueryable<TEntity> query,
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
        this IRavenQueryable<TEntity> query,
        CursorPagingArguments arguments,
        CancellationToken cancellationToken = default)
        => RavenCursorPagination<TEntity>.Instance.ApplyPaginationAsync(
            new RavenPagingContainer<TEntity>(query.ToAsyncDocumentQuery()),
            arguments,
            cancellationToken);

    /// <summary>
    /// Applies the cursor pagination algorithm to the <paramref name="query"/>.
    /// </summary>
    /// <param name="query">
    /// The query on which the the cursor pagination algorithm shall be applied to.
    /// </param>
    /// <param name="context">
    /// The field resolver context.
    /// </param>
    /// <param name="defaultPageSize">
    /// The default page size if no boundaries are set.
    /// </param>
    /// <param name="totalCount">
    /// The total count if already known.
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
        this IRavenQueryable<TEntity> query,
        IResolverContext context,
        int? defaultPageSize = null,
        int? totalCount = null,
        CancellationToken cancellationToken = default)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var first = context.ArgumentValue<int?>("first");
        var last = context.ArgumentValue<int?>("last");

        if (first is null && last is null)
        {
            first = defaultPageSize;
        }

        var arguments = new CursorPagingArguments(
            first,
            last,
            context.ArgumentValue<string?>("after"),
            context.ArgumentValue<string?>("before"));

        return RavenCursorPagination<TEntity>.Instance.ApplyPaginationAsync(
            new RavenPagingContainer<TEntity>(query.ToAsyncDocumentQuery()),
            arguments,
            totalCount,
            cancellationToken);
    }
}
