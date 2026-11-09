using System.Reflection;
using Elastic.Esql.Extensions;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.ElasticSearch.Pagination;

/// <summary>
/// An offset paging provider that reads the first page of an ES|QL query.
/// </summary>
public sealed class ElasticSearchOffsetPagingProvider : OffsetPagingProvider
{
    private static readonly MethodInfo s_createHandler =
        typeof(ElasticSearchOffsetPagingProvider).GetMethod(
            nameof(CreateHandlerInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <inheritdoc />
    public override bool CanHandle(IExtendedType source) => true;

    /// <inheritdoc />
    protected override OffsetPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);

        return (OffsetPagingHandler)s_createHandler
            .MakeGenericMethod(source.ElementType?.Source ?? source.Source)
            .Invoke(null, [options])!;
    }

    private static ElasticSearchOffsetPagingHandler<TEntity> CreateHandlerInternal<TEntity>(PagingOptions options)
        => new(options);

    private sealed class ElasticSearchOffsetPagingHandler<TEntity>(PagingOptions options) : OffsetPagingHandler(options)
    {
        private readonly ElasticSearchOffsetPagination<TEntity> _pagination = new();

        protected override async ValueTask<CollectionSegment> SliceAsync(
            IResolverContext context,
            object source,
            OffsetPagingArguments arguments)
        {
            var query = source switch
            {
                IQueryableExecutable<TEntity> executable => executable.Source,
                IQueryable<TEntity> queryable => queryable,
                _ => throw new GraphQLException("Cannot handle the specified data source.")
            };

            var requireTotalCount = IncludeTotalCount && context.IsSelected("totalCount");

            return await _pagination
                .ApplyPaginationAsync(query, arguments, requireTotalCount, context.RequestAborted)
                .ConfigureAwait(false);
        }
    }

    private sealed class ElasticSearchOffsetPagination<TEntity>
        : OffsetPaginationAlgorithm<IQueryable<TEntity>, TEntity>
    {
        protected override IQueryable<TEntity> ApplySkip(IQueryable<TEntity> query, int skip)
            => skip == 0 ? query : throw ThrowHelper.Paging_SkipNotSupported();

        protected override IQueryable<TEntity> ApplyTake(IQueryable<TEntity> query, int take)
            => query.Take(take);

        protected override async ValueTask<int> CountAsync(
            IQueryable<TEntity> query,
            CancellationToken cancellationToken)
            => await query.AsEsqlQueryable().CountAsync(cancellationToken).ConfigureAwait(false);

        protected override async ValueTask<IReadOnlyList<TEntity>> ExecuteAsync(
            IQueryable<TEntity> query,
            CancellationToken cancellationToken)
            => await query.AsEsqlQueryable().ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
