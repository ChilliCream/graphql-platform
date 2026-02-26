using System.Collections.Immutable;
using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.ElasticSearch.Paging;

internal sealed class ElasticSearchCursorPagingHandler<TEntity>(PagingOptions options)
    : CursorPagingHandler<IElasticSearchExecutable<TEntity>, TEntity>(options)
{
    private static readonly ElasticSearchCursorPagination<TEntity> s_pagination = new();
    private static readonly QueryExecutor s_executor = new();

    protected override ValueTask<Connection> SliceAsync(
        IResolverContext context,
        object source,
        CursorPagingArguments arguments)
        => SliceInternalAsync(context, source, arguments);

    private async ValueTask<Connection> SliceInternalAsync(
        IResolverContext context,
        object source,
        CursorPagingArguments arguments)
        => await SliceAsync(
                context,
                source as IElasticSearchExecutable<TEntity>
                    ?? throw ThrowHelper.PagingTypeNotSupported(source.GetType()),
                arguments,
                s_pagination,
                s_executor,
                context.RequestAborted)
            .ConfigureAwait(false);

    private sealed class QueryExecutor
        : ICursorPaginationQueryExecutor<IElasticSearchExecutable<TEntity>, TEntity>
    {
        public async ValueTask<int> CountAsync(
            IElasticSearchExecutable<TEntity> originalQuery,
            CancellationToken cancellationToken)
            => await originalQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        public async ValueTask<CursorPaginationData<TEntity>> QueryAsync(
            IElasticSearchExecutable<TEntity> slicedQuery,
            IElasticSearchExecutable<TEntity> originalQuery,
            int offset,
            bool includeTotalCount,
            CancellationToken cancellationToken)
        {
            if (includeTotalCount)
            {
                var itemsTask = slicedQuery.ToListAsync(cancellationToken).AsTask();
                var countTask = originalQuery.CountAsync(cancellationToken).AsTask();

                await Task.WhenAll(itemsTask, countTask).ConfigureAwait(false);

                if (itemsTask.IsCompletedSuccessfully && countTask.IsCompletedSuccessfully)
                {
                    return new CursorPaginationData<TEntity>(
                        CreateEdges(itemsTask.Result, offset),
                        countTask.Result);
                }

                return new(
                    CreateEdges(await itemsTask.ConfigureAwait(false), offset),
                    await countTask.ConfigureAwait(false));
            }

            var items = await slicedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);
            return new(CreateEdges(items, offset), null);
        }

        private static ImmutableArray<Edge<TEntity>> CreateEdges(
            IReadOnlyList<TEntity> items,
            int offset)
        {
            var builder = ImmutableArray.CreateBuilder<Edge<TEntity>>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                builder.Add(IndexEdge<TEntity>.Create(items[i], offset + i));
            }

            return builder.ToImmutable();
        }
    }
}
