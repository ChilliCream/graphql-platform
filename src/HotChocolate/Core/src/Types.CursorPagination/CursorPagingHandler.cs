using System.Collections.Immutable;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination.Utilities;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Pagination;

public abstract class CursorPagingHandler : IPagingHandler
{
    protected CursorPagingHandler(PagingOptions options)
    {
        DefaultPageSize =
            options.DefaultPageSize ?? PagingDefaults.DefaultPageSize;
        MaxPageSize =
            options.MaxPageSize ?? PagingDefaults.MaxPageSize;
        IncludeTotalCount =
            options.IncludeTotalCount ?? PagingDefaults.IncludeTotalCount;
        RequirePagingBoundaries =
            options.RequirePagingBoundaries ?? PagingDefaults.RequirePagingBoundaries;
        AllowBackwardPagination =
            options.AllowBackwardPagination ?? PagingDefaults.AllowBackwardPagination;

        if (MaxPageSize < DefaultPageSize)
        {
            DefaultPageSize = MaxPageSize;
        }
    }

    /// <summary>
    /// Gets the default page size.
    /// </summary>
    protected int DefaultPageSize { get; }

    /// <summary>
    /// Gets max allowed page size.
    /// </summary>
    protected int MaxPageSize { get; }

    /// <summary>
    /// Defines if the paging middleware shall require the
    /// API consumer to specify paging boundaries.
    /// </summary>
    protected bool RequirePagingBoundaries { get; }

    /// <summary>
    /// Result should include total count.
    /// </summary>
    protected bool IncludeTotalCount { get; }

    /// <summary>
    /// Defines if backward pagination is allowed or deactivated.
    /// </summary>
    protected bool AllowBackwardPagination { get; }

    public void ValidateContext(IResolverContext context)
    {
        var first = context.ArgumentValue<int?>(CursorPagingArgumentNames.First);
        var last = AllowBackwardPagination
            ? context.ArgumentValue<int?>(CursorPagingArgumentNames.Last)
            : null;

        if (RequirePagingBoundaries && first is null && last is null)
        {
            throw ThrowHelper.PagingHandler_NoBoundariesSet(
                context.Selection.Field,
                context.Path);
        }

        if (first < 0)
        {
            throw ThrowHelper.PagingHandler_MinPageSize(
                (int)first,
                context.Selection.Field,
                context.Path);
        }

        if (first > MaxPageSize)
        {
            throw ThrowHelper.PagingHandler_MaxPageSize(
                (int)first,
                MaxPageSize,
                context.Selection.Field,
                context.Path);
        }

        if (last < 0)
        {
            throw ThrowHelper.PagingHandler_MinPageSize(
                (int)last,
                context.Selection.Field,
                context.Path);
        }

        if (last > MaxPageSize)
        {
            throw ThrowHelper.PagingHandler_MaxPageSize(
                (int)last,
                MaxPageSize,
                context.Selection.Field,
                context.Path);
        }
    }

    public void PublishPagingArguments(IResolverContext context)
    {
        var first = context.ArgumentValue<int?>(CursorPagingArgumentNames.First);
        var last = AllowBackwardPagination
            ? context.ArgumentValue<int?>(CursorPagingArgumentNames.Last)
            : null;

        if (first is null && last is null)
        {
            first = DefaultPageSize;
        }

        var arguments = new CursorPagingArguments(
            first,
            last,
            context.ArgumentValue<string?>(CursorPagingArgumentNames.After),
            AllowBackwardPagination
                ? context.ArgumentValue<string?>(CursorPagingArgumentNames.Before)
                : null);

        context.SetLocalState(WellKnownContextData.PagingArguments, arguments);
    }

    async ValueTask<IPage> IPagingHandler.SliceAsync(
        IResolverContext context,
        object source)
    {
        var arguments = context.GetLocalState<CursorPagingArguments>(WellKnownContextData.PagingArguments);
        return await SliceAsync(context, source, arguments).ConfigureAwait(false);
    }

    protected abstract ValueTask<Connection> SliceAsync(
        IResolverContext context,
        object source,
        CursorPagingArguments arguments);
}

public abstract class CursorPagingHandler<TQuery, TEntity>(PagingOptions options)
    : CursorPagingHandler(options)
    where TQuery : notnull
{
    protected async ValueTask<Connection<TEntity>> SliceAsync(
        IResolverContext context,
        TQuery originalQuery,
        CursorPagingArguments arguments,
        CursorPaginationAlgorithm<TQuery, TEntity> algorithm,
        ICursorPaginationQueryExecutor<TQuery, TEntity> executor,
        CancellationToken cancellationToken)
    {
        var pagingFlags = context.GetPagingFlags(IncludeTotalCount);
        var countRequired = (pagingFlags & PagingFlags.TotalCount) == PagingFlags.TotalCount;
        var edgesRequired = (pagingFlags & PagingFlags.Edges) == PagingFlags.Edges;

        int? totalCount = null;
        if (arguments.Before is null && arguments.First is null)
        {
            totalCount = await executor.CountAsync(originalQuery, cancellationToken).ConfigureAwait(false);
            countRequired = false;
        }

        var (slicedQuery, offset, length) = algorithm.ApplyPagination(originalQuery, arguments, totalCount);

        // we store the original query and the sliced query in the
        // context for later use by customizations.
        context.SetOriginalQuery(originalQuery);
        context.SetSlicedQuery(slicedQuery);

        // if no edges are required we will return a connection without edges.
        if (!edgesRequired)
        {
            if (countRequired)
            {
                totalCount = await executor.CountAsync(originalQuery, cancellationToken).ConfigureAwait(false);
            }

            return new Connection<TEntity>(ConnectionPageInfo.Empty, totalCount ?? -1);
        }

        var data = await executor.QueryAsync(
            slicedQuery,
            originalQuery,
            offset,
            countRequired,
            cancellationToken).ConfigureAwait(false);

        var moreItemsReturnedThanRequested = data.Edges.Length > length;
        var isSequenceFromStart = offset == 0;
        var edges = data.Edges;

        if (moreItemsReturnedThanRequested)
        {
            edges = edges.Slice(0, length);
        }

        var pageInfo = CreatePageInfo(isSequenceFromStart, moreItemsReturnedThanRequested, edges);

        return new Connection<TEntity>(edges, pageInfo, totalCount ?? data.TotalCount ?? -1);
    }

    private static ConnectionPageInfo CreatePageInfo(
        bool isSequenceFromStart,
        bool moreItemsReturnedThanRequested,
        IReadOnlyList<Edge<TEntity>> selectedEdges)
    {
        // We know that there is a next page if more items than requested are returned
        var hasNextPage = moreItemsReturnedThanRequested;

        // There is a previous page if the sequence start is not 0.
        // If you point to index 2 of an empty list, we assume that there is a previous page
        var hasPreviousPage = !isSequenceFromStart;

        Edge<TEntity>? firstEdge = null;
        Edge<TEntity>? lastEdge = null;

        if (selectedEdges.Count > 0)
        {
            firstEdge = selectedEdges[0];
            lastEdge = selectedEdges[selectedEdges.Count - 1];
        }

        return new ConnectionPageInfo(
            hasNextPage,
            hasPreviousPage,
            firstEdge?.Cursor,
            lastEdge?.Cursor);
    }
}
