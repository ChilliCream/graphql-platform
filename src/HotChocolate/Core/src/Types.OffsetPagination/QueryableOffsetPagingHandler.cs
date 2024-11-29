using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents the default paging handler for in-memory collections and queryable.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type.
/// </typeparam>
public class QueryableOffsetPagingHandler<TEntity>(PagingOptions options)
    : OffsetPagingHandler(options)
{
    private readonly QueryableOffsetPagination<TEntity> _pagination = new();

    protected override ValueTask<CollectionSegment> SliceAsync(
        IResolverContext context,
        object source,
        OffsetPagingArguments arguments)
    {
        var ct = context.RequestAborted;
        return source switch
        {
            IQueryable<TEntity> q => ResolveAsync(context, q, arguments, ct),
            IEnumerable<TEntity> e => e.GetType().IsValueType
                ? throw new GraphQLException("Cannot handle the specified data source.")
                : ResolveAsync(context, e.AsQueryable(), arguments, ct),
            IExecutable<TEntity> ex => SliceAsync(context, ex.Source, arguments),
            _ => throw new GraphQLException("Cannot handle the specified data source."),
        };
    }

    private async ValueTask<CollectionSegment> ResolveAsync(
        IResolverContext context,
        IQueryable<TEntity> source,
        OffsetPagingArguments arguments = default,
        CancellationToken cancellationToken = default)
    {
        // TotalCount is one of the heaviest operations. It is only necessary to load totalCount
        // when it is enabled (IncludeTotalCount) and when it is contained in the selection set.
        var requireTotalCount = IncludeTotalCount && context.IsSelected("totalCount");

        return await _pagination
            .ApplyPaginationAsync(source, arguments, null, requireTotalCount, cancellationToken)
            .ConfigureAwait(false);
    }
}
