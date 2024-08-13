using System.Collections.Immutable;
using HotChocolate.Resolvers;
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
        // TotalCount is one of the heaviest operations. It is only necessary to load totalCount
        // when it is enabled (IncludeTotalCount) and when it is contained in the selection set.
        var totalCountRequired = IncludeTotalCount && context.IsSelected(ConnectionType.Names.TotalCount);

        // If nodes, edges, or pageInfo are selected, fetch the actual data.
        var selectionsRequired =
            context.IsSelected(ConnectionType.Names.Nodes)
            || context.IsSelected(ConnectionType.Names.Edges)
            || context.IsSelected(ConnectionType.Names.PageInfo);

        // If selections are required we're going to slice the query and fetch some data.
        if (selectionsRequired)
        {
            int? totalCount = null;
            if (arguments.Before is null && arguments.First is null)
            {
                totalCount = await executor.CountAsync(originalQuery, cancellationToken).ConfigureAwait(false);
                totalCountRequired = false;
            }

            var (slicedQuery, offset, length) = algorithm.ApplyPagination(originalQuery, arguments, totalCount);
            var data = await executor.QueryAsync(slicedQuery, offset, totalCountRequired, cancellationToken).ConfigureAwait(false);
            var moreItemsReturnedThanRequested = data.Edges.Length > length;
            var isSequenceFromStart = offset == 0;
            var edges = data.Edges;

            if (moreItemsReturnedThanRequested)
            {
#if NET7_OR_GREATER
                edges = edges.Slice(0, length);
#else
                var builder = ImmutableArray.CreateBuilder<Edge<TEntity>>(length);
                for (var i = 0; i < length; i++)
                {
                    builder.Add(edges[i]);
                }

                edges = builder.MoveToImmutable();
#endif
            }

            var pageInfo = CreatePageInfo(isSequenceFromStart, moreItemsReturnedThanRequested, edges);

            return new Connection<TEntity>(edges, pageInfo, data.TotalCount ?? -1);
        }

        // if we require a count but no data we will just run the count on the query.
        if (totalCountRequired)
        {
            var count = await executor.CountAsync(originalQuery, cancellationToken).ConfigureAwait(false);
            return new Connection<TEntity>(ImmutableArray<Edge<TEntity>>.Empty, ConnectionPageInfo.Empty, count);
        }

        // neither data nor totalCount is selected, so we return a dummy instance with no data for extensibility.
        return new Connection<TEntity>(ImmutableArray<Edge<TEntity>>.Empty, ConnectionPageInfo.Empty, -1);
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
