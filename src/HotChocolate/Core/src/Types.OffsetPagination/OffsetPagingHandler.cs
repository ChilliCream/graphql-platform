using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents an offset paging handler, which can be implemented to
/// create optimized pagination for data sources.
///
/// The paging handler will be used by the paging middleware to slice the data.
/// </summary>
public abstract class OffsetPagingHandler : IPagingHandler
{
    protected OffsetPagingHandler(PagingOptions options)
    {
        DefaultPageSize = options.DefaultPageSize ?? PagingDefaults.DefaultPageSize;
        MaxPageSize = options.MaxPageSize ?? PagingDefaults.MaxPageSize;
        IncludeTotalCount = options.IncludeTotalCount ?? PagingDefaults.IncludeTotalCount;
        RequirePagingBoundaries = options.RequirePagingBoundaries ?? false;

        if (MaxPageSize < DefaultPageSize)
        {
            DefaultPageSize = MaxPageSize;
        }
    }

    /// <summary>
    /// The default page size configured for this handler.
    /// </summary>
    protected int DefaultPageSize { get; }

    /// <summary>
    /// The maximum allowed page size configured for this handler.
    /// </summary>
    /// <value></value>
    protected int MaxPageSize { get; }

    /// <summary>
    /// Result should include total count.
    /// </summary>
    protected bool IncludeTotalCount { get; }

    /// <summary>
    /// Defines if the paging middleware shall require the
    /// API consumer to specify paging boundaries.
    /// </summary>
    protected bool RequirePagingBoundaries { get; }

    /// <summary>
    /// Ensures that the arguments passed in by the user are valid and
    /// do not try to consume more items per page as specified by
    /// <see cref="MaxPageSize"/>.
    /// </summary>
    /// <param name="context">
    /// The resolver context of the execution field.
    /// </param>
    public void ValidateContext(IResolverContext context)
    {
        var take = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Take);

        if (RequirePagingBoundaries && take is null)
        {
            throw ThrowHelper.OffsetPagingHandler_NoBoundariesSet(
                context.Selection.Field,
                context.Path);
        }

        if (take > MaxPageSize)
        {
            throw ThrowHelper.OffsetPagingHandler_MaxPageSize(
                take.Value,
                MaxPageSize,
                context.Selection.Field,
                context.Path);
        }
    }

    public void PublishPagingArguments(IResolverContext context)
    {
        var skip = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Skip);
        var take = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Take);
        var arguments = new OffsetPagingArguments(skip, take ?? DefaultPageSize);
        context.SetLocalState(WellKnownContextData.PagingArguments, arguments);
    }

    async ValueTask<IPage> IPagingHandler.SliceAsync(
        IResolverContext context,
        object source)
    {
        var arguments =context.GetLocalState<OffsetPagingArguments>(WellKnownContextData.PagingArguments);
        return await SliceAsync(context, source, arguments).ConfigureAwait(false);
    }

    /// <summary>
    /// The algorithm defining how to slice data of the specified <paramref name="source"/>.
    /// </summary>
    /// <param name="context">
    /// The resolver context of the execution field.
    /// </param>
    /// <param name="source">
    /// The object representing the data source, collection, or query builder.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments provided by the user.
    /// </param>
    /// <returns>
    /// The <see cref="CollectionSegment"/> representing
    /// the slice of items belonging to the requested page.
    /// </returns>
    protected abstract ValueTask<CollectionSegment> SliceAsync(
        IResolverContext context,
        object source,
        OffsetPagingArguments arguments);
}
