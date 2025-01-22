using System.Collections.Immutable;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination;

internal sealed class QueryableCursorPagingHandler<TEntity> : CursorPagingHandler<IQueryable<TEntity>, TEntity>
{
    private readonly bool _inlineTotalCount;

    public QueryableCursorPagingHandler(PagingOptions options) : this(options, false)
    {
    }

    public QueryableCursorPagingHandler(PagingOptions options, bool inlineTotalCount) : base(options)
    {
        _inlineTotalCount = inlineTotalCount;
    }

    private static readonly QueryableCursorPaginationAlgorithm<TEntity> _paginationAlgorithm =
        QueryableCursorPaginationAlgorithm<TEntity>.Instance;

    public ValueTask<Connection<TEntity>> SliceAsync(
        IResolverContext context,
        IQueryableExecutable<TEntity> source,
        CursorPagingArguments arguments)
        => SliceAsync(
            context,
            source.Source,
            arguments,
            _paginationAlgorithm,
            new QueryExecutor(source, _inlineTotalCount),
            context.RequestAborted);

    protected override ValueTask<Connection> SliceAsync(
        IResolverContext context,
        object source,
        CursorPagingArguments arguments)
        => source switch
        {
            IQueryable<TEntity> q => SliceAsyncInternal(context, Executable.From(q), arguments),
            IEnumerable<TEntity> e => e.GetType().IsValueType
                ? throw new GraphQLException("Cannot handle the specified data source.")
                : SliceAsyncInternal(context, Executable.From(e.AsQueryable()), arguments),
            IQueryableExecutable<TEntity> ex => SliceAsyncInternal(context, ex, arguments),
            _ => throw new GraphQLException("Cannot handle the specified data source."),
        };

    private async ValueTask<Connection> SliceAsyncInternal(
        IResolverContext context,
        IQueryableExecutable<TEntity> source,
        CursorPagingArguments arguments)
        => await SliceAsync(
                context,
                source.Source,
                arguments,
                _paginationAlgorithm,
                new QueryExecutor(source, _inlineTotalCount),
                context.RequestAborted)
            .ConfigureAwait(false);

    private sealed class QueryExecutor(IQueryableExecutable<TEntity> executable, bool inlineTotalCount)
        : ICursorPaginationQueryExecutor<IQueryable<TEntity>, TEntity>
    {
        public ValueTask<int> CountAsync(
            IQueryable<TEntity> originalQuery,
            CancellationToken cancellationToken)
            => executable.CountAsync(cancellationToken);

        public async ValueTask<CursorPaginationData<TEntity>> QueryAsync(
            IQueryable<TEntity> slicedQuery,
            IQueryable<TEntity> originalQuery,
            int offset,
            bool includeTotalCount,
            CancellationToken cancellationToken)
        {
            var totalCount = -1;
            var edges = ImmutableArray.CreateBuilder<Edge<TEntity>>();

            if (includeTotalCount)
            {
                if (inlineTotalCount)
                {
                    var combinedQuery = slicedQuery.Select(t => new { TotalCount = originalQuery.Count(), Item = t });
                    totalCount = 0;

                    var index = offset;
                    await foreach (var item in executable
                        .WithSource(combinedQuery)
                        .ToAsyncEnumerable(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        edges.Add(IndexEdge<TEntity>.Create(item.Item, index++));
                        totalCount = item.TotalCount;
                    }
                }
                else
                {
                    // if we cannot inline the total count we will do two queries.
                    // first we start without awaiting it the count of the original query.
                    var count = executable.CountAsync(cancellationToken).ConfigureAwait(false);

                    // next we collect the items that the sliced query produces.
                    var index = offset;
                    await foreach (var item in executable
                        .WithSource(slicedQuery)
                        .ToAsyncEnumerable(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        edges.Add(IndexEdge<TEntity>.Create(item, index++));
                    }

                    // last we await the count on the original query
                    totalCount = await count;
                }
            }
            else
            {
                var index = offset;
                await foreach (var item in executable
                    .WithSource(slicedQuery)
                    .ToAsyncEnumerable(cancellationToken)
                    .ConfigureAwait(false))
                {
                    edges.Add(IndexEdge<TEntity>.Create(item, index++));
                }
            }

            return new CursorPaginationData<TEntity>(edges.ToImmutable(), totalCount);
        }
    }

    public static QueryableCursorPagingHandler<TEntity> Default { get; } =
        new(new PagingOptions { IncludeTotalCount = true });
}
