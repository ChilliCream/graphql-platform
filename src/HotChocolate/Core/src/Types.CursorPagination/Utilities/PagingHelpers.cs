using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination.Utilities;

/// <summary>
/// A set of extension methods for <see cref="IResolverContext"/> to help with pagination customizations.
/// </summary>
public static class PagingHelpers
{
    private const string PagingFlagsKey = "HotChocolate.Pagination.Flags";
    private const string OriginalQuery = "HotChocolate.Pagination.OriginalQuery";
    private const string SlicedQuery = "HotChocolate.Pagination.SlicedQuery";

    /// <summary>
    /// Gets the paging flags from the context.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="totalCountEnabled">
    /// Defines weather the total count is enabled.
    /// </param>
    /// <returns>
    /// Returns the paging flags.
    /// </returns>
    public static PagingFlags GetPagingFlags(this IResolverContext context, bool totalCountEnabled = false)
    {
        var pagingFlags = context.GetLocalStateOrDefault(PagingFlagsKey, PagingFlags.None);

        // TotalCount is one of the heaviest operations. It is only necessary to load totalCount
        // when it is enabled (IncludeTotalCount) and when it is contained in the selection set.
        var totalCountRequired =
            totalCountEnabled
            && context.IsSelected(ConnectionType.Names.TotalCount);

        // If nodes, edges, or pageInfo are selected, fetch the actual data.
        var edgesRequired =
            context.IsSelected(ConnectionType.Names.Nodes)
            || context.IsSelected(ConnectionType.Names.Edges)
            || context.IsSelected(ConnectionType.Names.PageInfo);

        if (totalCountRequired)
        {
            pagingFlags |= PagingFlags.TotalCount;
        }

        if (edgesRequired)
        {
            pagingFlags |= PagingFlags.Edges;
        }

        return pagingFlags;
    }

    /// <summary>
    /// Sets the paging flags for the current resolver pipeline.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="flags">
    /// The paging flags.
    /// </param>
    public static void SetPagingFlags(this IResolverContext context, PagingFlags flags)
        => context.SetLocalState(PagingFlagsKey, flags);

    /// <summary>
    /// Sets the original query before the paging middleware appended any slicing.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="query">
    /// The original query.
    /// </param>
    /// <typeparam name="TQuery">
    /// The type of the query.
    /// </typeparam>
    public static void SetOriginalQuery<TQuery>(this IResolverContext context, TQuery query)
        => context.SetLocalState(OriginalQuery, query);

    /// <summary>
    /// Sets the sliced query after the paging middleware appended slicing.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="query">
    /// The sliced query.
    /// </param>
    /// <typeparam name="TQuery">
    /// The type of the query.
    /// </typeparam>
    public static void SetSlicedQuery<TQuery>(this IResolverContext context, TQuery query)
        => context.SetLocalState(SlicedQuery, query);

    /// <summary>
    /// Gets the original query before the paging middleware appended any slicing.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <typeparam name="TQuery">
    /// The type of the query.
    /// </typeparam>
    /// <returns>
    /// Returns the original query.
    /// </returns>
    public static TQuery GetOriginalQuery<TQuery>(this IResolverContext context)
        => context.GetLocalState<TQuery>(OriginalQuery);

    /// <summary>
    /// Gets the sliced query after the paging middleware appended slicing.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <typeparam name="TQuery">
    /// The type of the query.
    /// </typeparam>
    /// <returns>
    /// Returns the sliced query.
    /// </returns>
    public static TQuery GetSlicedQuery<TQuery>(this IResolverContext context)
        => context.GetLocalState<TQuery>(SlicedQuery);
}
