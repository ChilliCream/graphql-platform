using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination;

internal class QueryableCursorPagingHandler<TEntity> : CursorPagingHandler
{
    private readonly QueryableCursorPagination<TEntity> _pagination =
        QueryableCursorPagination<TEntity>.Instance;

    public QueryableCursorPagingHandler(PagingOptions options)
        : base(options)
    {
    }

    protected override ValueTask<Connection> SliceAsync(
        IResolverContext context,
        object source,
        CursorPagingArguments arguments)
    {
        var ct = context.RequestAborted;
        return source switch
        {
            IQueryable<TEntity> q => ResolveAsync(context, q, arguments, ct),
            IEnumerable<TEntity> e => ResolveAsync(context, e.AsQueryable(), arguments, ct),
            IExecutable<TEntity> ex => SliceAsync(context, ex.Source, arguments),
            _ => throw new GraphQLException("Cannot handle the specified data source."),
        };
    }

    private async ValueTask<Connection> ResolveAsync(
        IResolverContext context,
        IQueryable<TEntity> source,
        CursorPagingArguments arguments = default,
        CancellationToken cancellationToken = default)
    {
        // When totalCount is included in the selection set we prefetch it, then capture the
        // count in a variable, to pass it into the handler
        int? totalCount = null;

        // TotalCount is one of the heaviest operations. It is only necessary to load totalCount
        // when it is enabled (IncludeTotalCount) and when it is contained in the selection set.
        if (IncludeTotalCount && context.IsTotalCountSelected())
        {
            totalCount = source.Count();
        }

        return await _pagination
            .ApplyPaginationAsync(source, arguments, totalCount, cancellationToken)
            .ConfigureAwait(false);
    }
}
