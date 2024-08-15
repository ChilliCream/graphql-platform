using HotChocolate.Resolvers;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Pagination;

/// <summary>
/// Provides offset paging extensions to <see cref="IQueryable{T}"/>.
/// </summary>
public static class OffsetPagingQueryableExtensions
{
    /// <summary>
    /// Applies the offset pagination algorithm to the <paramref name="query"/>.
    /// </summary>
    /// <param name="query">
    /// The query on which the the offset pagination algorithm shall be applied to.
    /// </param>
    /// <param name="skip">
    /// Bypasses a _n_ elements from the list..
    /// </param>
    /// <param name="take">
    /// Returns the last _n_ elements from the list.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <typeparam name="TEntity">
    /// The entity type.
    /// </typeparam>
    /// <returns>
    /// Returns a collection segment instance that represents the result of applying the
    /// offset paging algorithm to the provided <paramref name="query"/>.
    /// </returns>
    public static ValueTask<CollectionSegment<TEntity>> ApplyOffsetPaginationAsync<TEntity>(
        this IQueryable<TEntity> query,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
        => ApplyOffsetPaginationAsync(
            query,
            new OffsetPagingArguments(skip, take),
            cancellationToken);

    /// <summary>
    /// Applies the offset pagination algorithm to the <paramref name="query"/>.
    /// </summary>
    /// <param name="query">
    /// The query on which the the offset pagination algorithm shall be applied to.
    /// </param>
    /// <param name="arguments">
    /// The offset paging arguments.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <typeparam name="TEntity">
    /// The entity type.
    /// </typeparam>
    /// <returns>
    /// Returns a collection segment instance that represents the result of applying the
    /// offset paging algorithm to the provided <paramref name="query"/>.
    /// </returns>
    public static ValueTask<CollectionSegment<TEntity>> ApplyOffsetPaginationAsync<TEntity>(
        this IQueryable<TEntity> query,
        OffsetPagingArguments arguments,
        CancellationToken cancellationToken = default)
        => QueryableOffsetPagination<TEntity>.Instance.ApplyPaginationAsync(
            query,
            arguments,
            false,
            cancellationToken);

    /// <summary>
    /// Applies the offset pagination algorithm to the <paramref name="query"/>.
    /// </summary>
    /// <param name="query">
    /// The query on which the the offset pagination algorithm shall be applied to.
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
    /// <param name="requireTotalCount">Specifies if the total count is needed.</param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <typeparam name="TEntity">
    /// The entity type.
    /// </typeparam>
    /// <returns>
    /// Returns a collection segment instance that represents the result of applying the
    /// offset paging algorithm to the provided <paramref name="query"/>.
    /// </returns>
    public static ValueTask<CollectionSegment<TEntity>> ApplyOffsetPaginationAsync<TEntity>(
        this IQueryable<TEntity> query,
        IResolverContext context,
        int? defaultPageSize = null,
        int? totalCount = null,
        bool requireTotalCount = false,
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

        var skip = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Skip);
        var take = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Take) ?? defaultPageSize;
        var arguments = new OffsetPagingArguments(skip, take);

        if (totalCount is null && context.IsTotalCountSelected())
        {
            totalCount = query.Count();
        }

        return QueryableOffsetPagination<TEntity>.Instance.ApplyPaginationAsync(
            query,
            arguments,
            totalCount,
            requireTotalCount,
            cancellationToken);
    }
}
