using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Paging;

internal class MongoDbCursorPagingHandler<TEntity>(PagingOptions options)
    : CursorPagingHandler<IMongoPagingContainer<TEntity>, TEntity>(options)
{
    private static readonly MongoCursorPaginationAlgorithm<TEntity> _paginationAlgorithmAlgorithm = new();
    private static readonly QueryExecutor _executor = new();

    protected override ValueTask<Connection> SliceAsync(
        IResolverContext context,
        object source,
        CursorPagingArguments arguments)
        => SliceInternalAsync(
            context,
            CreatePagingContainer(source),
            arguments);

    private async ValueTask<Connection> SliceInternalAsync(
        IResolverContext context,
        IMongoPagingContainer<TEntity> source,
        CursorPagingArguments arguments)
        => await SliceAsync(
                context,
                source,
                arguments,
                _paginationAlgorithmAlgorithm,
                _executor,
                context.RequestAborted)
            .ConfigureAwait(false);


    private static IMongoPagingContainer<TEntity> CreatePagingContainer(object source)
    {
        return source switch
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

    private sealed class QueryExecutor : ICursorPaginationQueryExecutor<IMongoPagingContainer<TEntity>, TEntity>
    {
        public async ValueTask<int> CountAsync(
            IMongoPagingContainer<TEntity> originalQuery,
            CancellationToken cancellationToken)
            => await originalQuery.CountAsync(cancellationToken);

        public async ValueTask<CursorPaginationData<TEntity>> QueryAsync(
            IMongoPagingContainer<TEntity> slicedQuery,
            IMongoPagingContainer<TEntity> originalQuery,
            int offset,
            bool includeTotalCount,
            CancellationToken cancellationToken)
        {
            if (includeTotalCount)
            {
                var itemsTask = slicedQuery.QueryAsync(offset, cancellationToken);
                var countTask = originalQuery.CountAsync(cancellationToken);

                await Task.WhenAll(itemsTask, countTask).ConfigureAwait(false);

                if (itemsTask.IsCompletedSuccessfully && countTask.IsCompletedSuccessfully)
                {
                    return new CursorPaginationData<TEntity>(
                        itemsTask.Result,
                        countTask.Result);
                }

                // if the tasks were not completed successfully we need to await them again
                // to propagate exceptions.
                return new(await itemsTask, await countTask);
            }

            var items = await slicedQuery.QueryAsync(offset, cancellationToken).ConfigureAwait(false);
            return new(items, null);
        }
    }
}
