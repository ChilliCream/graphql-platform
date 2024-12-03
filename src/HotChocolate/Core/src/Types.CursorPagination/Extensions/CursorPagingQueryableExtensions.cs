using HotChocolate.Resolvers;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Pagination;

/// <summary>
/// Provides cursor paging extensions to <see cref="IQueryable{T}"/>.
/// </summary>
public static class CursorPagingQueryableExtensions
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
        this IQueryable<TEntity> query,
        IResolverContext context,
        int? defaultPageSize = null)
        => ApplyCursorPaginationAsync(Executable.From(query), context, defaultPageSize);

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
        this IQueryableExecutable<TEntity> query,
        IResolverContext context,
        int? defaultPageSize = null)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var first = context.ArgumentValue<int?>(CursorPagingArgumentNames.First);
        var last = context.ArgumentValue<int?>(CursorPagingArgumentNames.Last);

        if (first is null && last is null)
        {
            first = defaultPageSize;
        }

        var arguments = new CursorPagingArguments(
            first,
            last,
            context.ArgumentValue<string?>(CursorPagingArgumentNames.After),
            context.ArgumentValue<string?>(CursorPagingArgumentNames.Before));

        return QueryableCursorPagingHandler<TEntity>.Default.SliceAsync(context, query, arguments);
    }

    /// <summary>
    /// Applies the cursor pagination algorithm to the <paramref name="enumerable"/>.
    /// </summary>
    /// <param name="enumerable">
    /// The enumerable on which the cursor pagination algorithm shall be applied to.
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
    /// cursor paging algorithm to the provided <paramref name="enumerable"/>.
    /// </returns>
    public static ValueTask<Connection<TEntity>> ApplyCursorPaginationAsync<TEntity>(
        this IEnumerable<TEntity> enumerable,
        IResolverContext context,
        int? defaultPageSize = null)
        => ApplyCursorPaginationAsync(enumerable.AsQueryable(), context, defaultPageSize);
}
