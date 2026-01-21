using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.ElasticSearch.Paging;

public class ElasticSearchCursorPagingHandler<TEntity> : CursorPagingHandler
{
    private readonly ElasticSearchCursorPagination<TEntity> _pagination = new();

    public ElasticSearchCursorPagingHandler(PagingOptions options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask<Connection> SliceAsync(
        IResolverContext context,
        object source,
        CursorPagingArguments arguments)
        => await _pagination
            .ApplyPaginationAsync(
                source as IElasticSearchExecutable<TEntity> ??
                    throw ThrowHelper.PagingTypeNotSupported(source.GetType()),
                arguments,
                context.RequestAborted)
            .ConfigureAwait(false);
}
