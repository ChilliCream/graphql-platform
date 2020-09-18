using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// Represents the default paging handler for in-memory collections and queryables.
    /// </summary>
    /// <typeparam name="TItemType">
    /// The entity type.
    /// </typeparam>
    public class QueryableOffsetPagingHandler<TItemType>
        : OffsetPagingHandler
    {
        public QueryableOffsetPagingHandler(PagingOptions options)
            : base(options)
        {
        }

        protected override async ValueTask<CollectionSegment> SliceAsync(
            IResolverContext context,
            object source,
            OffsetPagingArguments arguments)
        {
            IQueryable<TItemType> queryable = source switch
            {
                IQueryable<TItemType> q => q,
                IEnumerable<TItemType> e => e.AsQueryable(),
                _ => throw new GraphQLException("Cannot handle the specified data source.")
            };

            IQueryable<TItemType> original = queryable;

            if (arguments.Skip.HasValue)
            {
                queryable = queryable.Skip(arguments.Skip.Value);
            }

            queryable = queryable.Take(arguments.Take + 1);
            List<TItemType> items =
                await ExecuteQueryableAsync(queryable, context.RequestAborted)
                    .ConfigureAwait(false);
            var pageInfo = new CollectionSegmentInfo(
                items.Count == arguments.Take + 1,
                (arguments.Skip ?? 0) > 0);

            if (items.Count > arguments.Take)
            {
                items.RemoveAt(arguments.Take);
            }

            return new CollectionSegment((IReadOnlyCollection<object>)items, pageInfo, CountAsync);

            async ValueTask<int> CountAsync(CancellationToken cancellationToken) =>
                await Task.Run(original.Count, cancellationToken).ConfigureAwait(false);
        }

        protected virtual async ValueTask<List<TItemType>> ExecuteQueryableAsync(
            IQueryable<TItemType> queryable,
            CancellationToken cancellationToken)
        {
            var list = new List<TItemType>();

            if (queryable is IAsyncEnumerable<TItemType> enumerable)
            {
                await foreach (TItemType item in enumerable.WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    list.Add(item);
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    foreach (TItemType item in queryable)
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
    }
}
