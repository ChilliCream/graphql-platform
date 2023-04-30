using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.ElasticSearch.Paging;

public class ElasticSearchOffsetPagingHandler<TEntity> : OffsetPagingHandler
{
    private readonly ElasticSearchOffsetPagination<TEntity> _pagination = new();

    /// <inheritdoc />
    public ElasticSearchOffsetPagingHandler(PagingOptions options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask<CollectionSegment> SliceAsync(
        IResolverContext context,
        object source,
        OffsetPagingArguments arguments)
        => await _pagination.ApplyPaginationAsync(
                CreatePagingContainer(source),
                arguments,
                context.RequestAborted)
            .ConfigureAwait(false);

    private IElasticSearchPagingContainer<TEntity> CreatePagingContainer(object source)
    {
        return source switch
        {
            IElasticSearchExecutable<TEntity> e =>
                new ElasticSearchExecutablePagingContainer<TEntity>(e),
            _ => throw ThrowHelper.PagingTypeNotSupported(source.GetType())
        };
    }
}
