using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Pagination;

internal sealed class RavenCursorPagingHandler<TEntity> : CursorPagingHandler
{
    private readonly RavenCursorPagination<TEntity> _pagination = new();

    public RavenCursorPagingHandler(PagingOptions options) : base(options)
    {
    }

    protected override async ValueTask<Connection> SliceAsync(
        IResolverContext context,
        object source,
        CursorPagingArguments arguments)
        => await _pagination.ApplyPaginationAsync(
                CreatePagingContainer(source),
                arguments,
                context.RequestAborted)
            .ConfigureAwait(false);

    private static RavenPagingContainer<TEntity> CreatePagingContainer(object source)
        => new(source switch
        {
            RavenAsyncDocumentQueryExecutable<TEntity> e => e.Query,
            IRavenQueryable<TEntity> e => e.ToAsyncDocumentQuery(),
            IAsyncDocumentQuery<TEntity> f => f,
            _ => throw ThrowHelper.PagingTypeNotSupported(source.GetType()),
        });
}
