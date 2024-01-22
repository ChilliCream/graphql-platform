using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Pagination;

internal sealed class RavenOffsetPagingHandler<TEntity> : OffsetPagingHandler
{
    private readonly RavenOffsetPagination<TEntity> _pagination = new();

    public RavenOffsetPagingHandler(PagingOptions options) : base(options)
    {
    }

    protected override async ValueTask<CollectionSegment> SliceAsync(
        IResolverContext context,
        object source,
        OffsetPagingArguments arguments)
        => await _pagination.ApplyPaginationAsync(
                CreatePagingContainer(source),
                arguments,
                context.RequestAborted)
            .ConfigureAwait(false);

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
