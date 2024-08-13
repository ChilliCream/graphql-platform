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
    /// The query on which the cursor pagination algorithm shall be applied to.
    /// </param>
    /// <param name="context">
    /// The field resolver context.
    /// </param>
    /// <param name="defaultPageSize">
    /// The default page size if no boundaries are set.
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
        int? defaultPageSize = null)
        => ApplyCursorPaginationAsync(
            new RavenPagingContainer<TEntity>(query.ToAsyncDocumentQuery()),
            context,
            defaultPageSize);

     /// <summary>
    /// Applies the cursor pagination algorithm to the <paramref name="query"/>.
    /// </summary>
    /// <param name="query">
    /// The query on which the cursor pagination algorithm shall be applied to.
    /// </param>
    /// <param name="context">
    /// The field resolver context.
    /// </param>
    /// <param name="defaultPageSize">
    /// The default page size if no boundaries are set.
    /// </param>
    /// <typeparam name="TEntity">
    /// The entity type.
    /// </typeparam>
    /// <returns>
    /// Returns a connection instance that represents the result of applying the
    /// cursor paging algorithm to the provided <paramref name="query"/>.
    /// </returns>
    internal static ValueTask<Connection<TEntity>> ApplyCursorPaginationAsync<TEntity>(
        this RavenPagingContainer<TEntity> query,
        IResolverContext context,
        int? defaultPageSize)
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

        return RavenCursorPagingHandler<TEntity>.Default.SliceAsync(
            context,
            query,
            arguments);
    }
}
