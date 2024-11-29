using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Paging;

internal class MongoDbOffsetPagingHandler<TEntity>(PagingOptions options)
    : OffsetPagingHandler(options)
{
    private readonly MongoOffsetPagination<TEntity> _pagination = new();

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

    private static IMongoPagingContainer<TEntity> CreatePagingContainer(object source) =>
        source switch
        {
            IAggregateFluent<TEntity> e => AggregateFluentPagingContainer<TEntity>.New(e),
            IFindFluent<TEntity, TEntity> f => FindFluentPagingContainer<TEntity>.New(f),
            IMongoCollection<TEntity> m => FindFluentPagingContainer<TEntity>.New(
                m.Find(FilterDefinition<TEntity>.Empty)),
            MongoDbCollectionExecutable<TEntity> mce => CreatePagingContainer(mce.BuildPipeline()),
            MongoDbAggregateFluentExecutable<TEntity> mae => CreatePagingContainer(mae.BuildPipeline()),
            MongoDbFindFluentExecutable<TEntity> mfe => CreatePagingContainer(mfe.BuildPipeline()),
            _ => throw ThrowHelper.PagingTypeNotSupported(source.GetType()),
        };
}
