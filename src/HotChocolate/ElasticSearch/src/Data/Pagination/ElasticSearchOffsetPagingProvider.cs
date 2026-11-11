using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elastic.Esql.Core;
using Elastic.Esql.Extensions;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.ElasticSearch.Pagination;

/// <summary>
/// An offset paging provider that reads the first page of an ES|QL query.
/// Other sources are paged by <see cref="QueryableOffsetPagingProvider"/>.
/// </summary>
[Experimental(ElasticSearchExperiments.Provider)]
public sealed class ElasticSearchOffsetPagingProvider : OffsetPagingProvider
{
    private static readonly MethodInfo s_createHandler =
        typeof(ElasticSearchOffsetPagingProvider).GetMethod(
            nameof(CreateHandlerInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly IPagingProvider s_fallback = new QueryableOffsetPagingProvider();

    /// <inheritdoc />
    /// <remarks>
    /// A source that is neither a list nor a queryable, such as a MongoDB executable, is left to its own provider.
    /// </remarks>
    public override bool CanHandle(IExtendedType source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return s_fallback.CanHandle(source)
            || source.Source.GetInterfaces().Append(source.Source).Any(
                type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryableExecutable<>));
    }

    /// <inheritdoc />
    protected override OffsetPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);

        return (OffsetPagingHandler)s_createHandler
            .MakeGenericMethod(source.ElementType?.Source ?? source.Source)
            .Invoke(null, [options, s_fallback.CreateHandler(source, options)])!;
    }

    private static ElasticSearchOffsetPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options,
        IPagingHandler fallback)
        => new(options, fallback);

    private sealed class ElasticSearchOffsetPagingHandler<TEntity>(PagingOptions options, IPagingHandler fallback)
        : OffsetPagingHandler(options)
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
                _ => null
            };

            if (query?.Provider is not EsqlQueryProvider)
            {
                return (CollectionSegment)await fallback.SliceAsync(context, source).ConfigureAwait(false);
            }

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
