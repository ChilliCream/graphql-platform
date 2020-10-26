using Cashflow.Cloud.GraphQLServer.MongoDb.Execution;
using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.MongoDb.Paging
{
    public class AggregateFluentOffsetPagingHandler<TItemType>
        : OffsetPagingHandler
    {
        public AggregateFluentOffsetPagingHandler(PagingOptions options)
            : base(options)
        {
            
        }

        protected override async ValueTask<CollectionSegment> SliceAsync(
            IResolverContext context,
            object source,
            OffsetPagingArguments arguments)
        {
            var aggregateFluent = source as IAggregateFluentExecutable<TItemType>;
            if (aggregateFluent == null)
            {
                throw new GraphQLException("Cannot handle the specified data source.");
            }    

            var original = aggregateFluent;

            if (arguments.Skip.HasValue)
            {
                aggregateFluent = aggregateFluent.Skip(arguments.Skip.Value);
            }

            aggregateFluent = aggregateFluent.Limit(arguments.Take + 1);

            var items = await aggregateFluent.ToListAsync(context.RequestAborted);

            var pageInfo = new CollectionSegmentInfo(
                items.Count == arguments.Take + 1,
                (arguments.Skip ?? 0) > 0);

            if (items.Count > arguments.Take)
            {
                items.RemoveAt(arguments.Take);
            }

            return new CollectionSegment((IReadOnlyCollection<object>)items, pageInfo, CountAsync);

            async ValueTask<int> CountAsync(CancellationToken cancellationToken) =>
                (int)((await original.Count().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false))?.Count ?? 0);
        }
    }
}
