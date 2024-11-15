using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Pagination;

internal sealed class RavenCursorPagingHandler<TEntity>(PagingOptions options)
    : CursorPagingHandler<RavenPagingContainer<TEntity>, TEntity>(options)
{
    private static readonly QueryExecutor _executor = new();
    private readonly RavenCursorPaginationAlgorithm<TEntity> _paginationAlgorithm = new();

    public ValueTask<Connection<TEntity>> SliceAsync(
        IResolverContext context,
        RavenPagingContainer<TEntity> source,
        CursorPagingArguments arguments)
        => SliceAsync(
            context,
            source,
            arguments,
            _paginationAlgorithm,
            _executor,
            context.RequestAborted);

    protected override ValueTask<Connection> SliceAsync(
        IResolverContext context,
        object source,
        CursorPagingArguments arguments)
        => SliceAsyncInternal(context, CreatePagingContainer(source), arguments);

    private async ValueTask<Connection> SliceAsyncInternal(
        IResolverContext context,
        RavenPagingContainer<TEntity> source,
        CursorPagingArguments arguments)
        => await SliceAsync(
                context,
                source,
                arguments,
                _paginationAlgorithm,
                _executor,
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

    private sealed class QueryExecutor : ICursorPaginationQueryExecutor<RavenPagingContainer<TEntity>, TEntity>
    {
        public async ValueTask<int> CountAsync(
            RavenPagingContainer<TEntity> originalQuery,
            CancellationToken cancellationToken)
            => await originalQuery.CountAsync(cancellationToken);

        public async ValueTask<CursorPaginationData<TEntity>> QueryAsync(
            RavenPagingContainer<TEntity> slicedQuery,
            RavenPagingContainer<TEntity> originalQuery,
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
                    return new(itemsTask.Result, countTask.Result);
                }

                // if the tasks were not completed successfully we need to await them again
                // to propagate exceptions.
                return new(await itemsTask, await countTask);
            }

            var items = await slicedQuery.QueryAsync(offset, cancellationToken).ConfigureAwait(false);
            return new(items, null);
        }
    }

    internal static RavenCursorPagingHandler<TEntity> Default { get; } =
        new(new PagingOptions { IncludeTotalCount = true });
}
