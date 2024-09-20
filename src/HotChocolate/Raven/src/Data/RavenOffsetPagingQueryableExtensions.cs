using HotChocolate.Data.Raven.Pagination;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace HotChocolate.Data;

/// <summary>
/// Provides offset paging extensions to <see cref="IRavenQueryable{T}"/>.
/// </summary>
public static class RavenOffsetPagingQueryableExtensions
{
    /// <summary>
    /// Applies the offset pagination algorithm to the <paramref name="query"/>.
    /// </summary>
    /// <param name="query">
    /// The query on which the offset pagination algorithm shall be applied to.
    /// </param>
    /// <param name="skip">
    /// Bypasses a _n_ elements from the list.
    /// </param>
    /// <param name="take">
    /// Returns the last _n_ elements from the list.
    /// </param>
    /// <param name="requireTotalCount">
    /// Specifies if the total count is needed.
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
        this IRavenQueryable<TEntity> query,
        int? skip = null,
        int? take = null,
        bool requireTotalCount = false,
        CancellationToken cancellationToken = default)
        => ApplyOffsetPaginationAsync(
            query,
            new OffsetPagingArguments(skip, take),
            requireTotalCount,
            cancellationToken);

    /// <summary>
    /// Applies the offset pagination algorithm to the <paramref name="query"/>.
    /// </summary>
    /// <param name="query">
    /// The query on which the offset pagination algorithm shall be applied to.
    /// </param>
    /// <param name="arguments">
    /// The offset paging arguments.
    /// </param>
    /// <param name="requireTotalCount">
    /// Specifies if the total count is needed.
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
        this IRavenQueryable<TEntity> query,
        OffsetPagingArguments arguments,
        bool requireTotalCount = false,
        CancellationToken cancellationToken = default)
        => RavenOffsetPagination<TEntity>.Instance.ApplyPaginationAsync(
            new RavenPagingContainer<TEntity>(query.ToAsyncDocumentQuery()),
            arguments,
            requireTotalCount,
            cancellationToken);

    /// <summary>
    /// Applies the offset pagination algorithm to the <paramref name="query"/>.
    /// </summary>
    /// <param name="query">
    /// The query on which the offset pagination algorithm shall be applied to.
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
    /// <param name="requireTotalCount">
    /// Specifies if the total count is needed.
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
        this IRavenQueryable<TEntity> query,
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

        var skip = context.ArgumentValue<int?>("skip");
        var take = context.ArgumentValue<int?>("take") ?? defaultPageSize;
        var arguments = new OffsetPagingArguments(skip, take);

        return RavenOffsetPagination<TEntity>.Instance.ApplyPaginationAsync(
            new RavenPagingContainer<TEntity>(query.ToAsyncDocumentQuery()),
            arguments,
            totalCount,
            requireTotalCount,
            cancellationToken);
    }
}
