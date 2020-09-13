using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Pagination
{
    public class OffsetPagingProvider
    {

    }

    public abstract class OffsetPagingHandler: IPagingHandler
    {
        protected OffsetPagingHandler(PagingSettings settings)
        {
            DefaultPageSize = settings.DefaultPageSize ?? PagingDefaults.DefaultPageSize;
            MaxPageSize = settings.MaxPageSize ?? PagingDefaults.MaxPageSize;
            IncludeTotalCount = settings.IncludeTotalCount ?? PagingDefaults.IncludeTotalCount;
        }

        protected int DefaultPageSize { get; }

        protected  int MaxPageSize { get; }

        protected  bool IncludeTotalCount { get; }

        public void ValidateContext(IResolverContext context)
        {
            int? take = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Take);

            if (take > MaxPageSize)
            {
                throw ThrowHelper.OffsetPagingHandler_MaxPageSize();
            }
        }

        async ValueTask<IPage> IPagingHandler.SliceAsync(
            IResolverContext context,
            object source)
        {
            int? skip = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Skip);
            int? take = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Take);


            var arguments = new OffsetPagingArguments(
                skip ?? 0,
                take ??DefaultPageSize);

            return await SliceAsync(context, source, arguments).ConfigureAwait(false);
        }

        protected abstract ValueTask<CollectionSegment> SliceAsync(
            IResolverContext context,
            object source,
            OffsetPagingArguments arguments);
    }

    public class QueryableOffsetPagingHandler<TEntity>
        : OffsetPagingHandler
    {
        public QueryableOffsetPagingHandler(PagingSettings settings)
            : base(settings)
        {
        }

        protected override async ValueTask<CollectionSegment> SliceAsync(
            IResolverContext context,
            object source,
            OffsetPagingArguments arguments)
        {
            IQueryable<TEntity> queryable = source switch
            {
                IQueryable<TEntity> q => q,
                IEnumerable<TEntity> e => e.AsQueryable(),
                _ => throw new GraphQLException("Cannot handle the specified data source.")
            };

            int? count = IncludeTotalCount
                ? (int?)await Task.Run(queryable.Count, context.RequestAborted)
                    .ConfigureAwait(false)
                : null;


            IQueryable<TClrType> slice = source;

            if (skip != null)
                slice = slice.Skip(skip.Value);
            if (take != null)
                slice = slice.Take(take.Value);

            context.Result = new CollectionSegment(, totalCount);
        }

        protected virtual async ValueTask<IReadOnlyList<TEntity>> ExecuteQueryableAsync(
            IQueryable<TEntity> queryable,
            CancellationToken cancellationToken)
        {
            var list = new List<TEntity>();

            if (queryable is IAsyncEnumerable<TEntity> enumerable)
            {
                await foreach (TEntity item in enumerable.WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    list.Add(item);
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    foreach (TEntity item in queryable)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        list.Add(item);
                    }

                }).ConfigureAwait(false);
            }

            return list;

        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {


            if (source != null)
            {
                int? skip = context.Argument<int?>("skip");
                int? take = context.Argument<int?>("take");


            }
        }


    }
}
