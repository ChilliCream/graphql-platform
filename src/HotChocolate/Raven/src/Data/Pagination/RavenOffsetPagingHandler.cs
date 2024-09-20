using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Pagination;

internal sealed class RavenOffsetPagingHandler<TEntity>(PagingOptions options) : OffsetPagingHandler(options)
{
    private readonly RavenOffsetPagination<TEntity> _pagination = new();

    protected override async ValueTask<CollectionSegment> SliceAsync(
        IResolverContext context,
        object source,
        OffsetPagingArguments arguments)
    {
        // TotalCount is one of the heaviest operations. It is only necessary to load totalCount
        // when it is enabled (IncludeTotalCount) and when it is contained in the selection set.
        var requireTotalCount = IncludeTotalCount && context.IsSelected("totalCount");

        return await _pagination.ApplyPaginationAsync(
                CreatePagingContainer(source),
                arguments,
                requireTotalCount,
                context.RequestAborted)
            .ConfigureAwait(false);
    }

    private static RavenPagingContainer<TEntity> CreatePagingContainer(object source)
    {
        return new RavenPagingContainer<TEntity>(source switch
        {
            RavenAsyncDocumentQueryExecutable<TEntity> e => e.Query,
            IRavenQueryable<TEntity> e => e.ToAsyncDocumentQuery(),
            IAsyncDocumentQuery<TEntity> f => f,
            _ => throw ThrowHelper.PagingTypeNotSupported(source.GetType()),
        });
    }
}
