using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Pagination;

public abstract class CursorPagingHandler : IPagingHandler
{
    protected CursorPagingHandler(PagingOptions options)
    {
        DefaultPageSize =
            options.DefaultPageSize ??
                PagingDefaults.DefaultPageSize;
        MaxPageSize =
            options.MaxPageSize ??
                PagingDefaults.MaxPageSize;
        IncludeTotalCount =
            options.IncludeTotalCount ??
                PagingDefaults.IncludeTotalCount;
        RequirePagingBoundaries =
            options.RequirePagingBoundaries ??
                PagingDefaults.RequirePagingBoundaries;
        AllowBackwardPagination =
            options.AllowBackwardPagination ??
                PagingDefaults.AllowBackwardPagination;

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

        if (first > MaxPageSize)
        {
            throw ThrowHelper.PagingHandler_MaxPageSize(
                (int)first,
                MaxPageSize,
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
