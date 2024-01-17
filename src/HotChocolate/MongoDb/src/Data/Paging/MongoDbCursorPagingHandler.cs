using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Paging;

internal class MongoDbCursorPagingHandler<TEntity> : CursorPagingHandler
{
    private readonly MongoCursorPagination<TEntity> _pagination = new();

    public MongoDbCursorPagingHandler(PagingOptions options) : base(options)
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

    private IMongoPagingContainer<TEntity> CreatePagingContainer(object source)
    {
        return source switch
        {
            IAggregateFluent<TEntity> e => AggregateFluentPagingContainer<TEntity>.New(e),
            IFindFluent<TEntity, TEntity> f => FindFluentPagingContainer<TEntity>.New(f),
            IMongoCollection<TEntity> m => FindFluentPagingContainer<TEntity>.New(
                m.Find(FilterDefinition<TEntity>.Empty)),
            MongoDbCollectionExecutable<TEntity> mce =>
                CreatePagingContainer(mce.BuildPipeline()),
            MongoDbAggregateFluentExecutable<TEntity> mae =>
                CreatePagingContainer(mae.BuildPipeline()),
            MongoDbFindFluentExecutable<TEntity> mfe =>
                CreatePagingContainer(mfe.BuildPipeline()),
            _ => throw ThrowHelper.PagingTypeNotSupported(source.GetType()),
        };
    }
}
