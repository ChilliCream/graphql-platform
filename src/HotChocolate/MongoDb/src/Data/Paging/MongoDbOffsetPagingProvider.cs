using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Paging
{
    public class MongoDbOffsetPagingProvider : OffsetPagingProvider
    {
        private static readonly MethodInfo _createHandler =
            typeof(MongoDbOffsetPagingProvider).GetMethod(
                nameof(CreateHandlerInternal),
                BindingFlags.Static | BindingFlags.NonPublic)!;

        public override bool CanHandle(IExtendedType source)
        {
            return true;
        }

        protected override OffsetPagingHandler CreateHandler(
            IExtendedType source,
            PagingOptions options)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return (OffsetPagingHandler)_createHandler
                .MakeGenericMethod(source.ElementType?.Source ?? source.Source)
                .Invoke(null, new object[] { options })!;
        }

        private static MongoDbOffsetPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
            PagingOptions options) =>
            new MongoDbOffsetPagingHandler<TEntity>(options);


        private class MongoDbOffsetPagingHandler<TEntity> : OffsetPagingHandler
        {
            public MongoDbOffsetPagingHandler(PagingOptions options) : base(options)
            {
            }

            protected override ValueTask<CollectionSegment> SliceAsync(
                IResolverContext context,
                object source,
                OffsetPagingArguments arguments)
            {
                IMongoPagingContainer<TEntity> f = CreatePagingContainer(source);
                return ResolveAsync(context, f, arguments);
            }

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
                    _ => throw ThrowHelper.PagingTypeNotSupported(source.GetType())
                };
            }

            private async ValueTask<CollectionSegment> ResolveAsync(
                IResolverContext context,
                IMongoPagingContainer<TEntity> queryable,
                OffsetPagingArguments arguments = default)
            {
                IMongoPagingContainer<TEntity> original = queryable;

                if (arguments.Skip.HasValue)
                {
                    queryable = queryable.Skip(arguments.Skip.Value);
                }

                queryable = queryable.Take(arguments.Take + 1);

                List<TEntity> items = await queryable
                    .ToListAsync(context.RequestAborted)
                    .ConfigureAwait(false);

                var pageInfo = new CollectionSegmentInfo(
                    items.Count == arguments.Take + 1,
                    (arguments.Skip ?? 0) > 0);

                if (items.Count > arguments.Take)
                {
                    items.RemoveAt(arguments.Take);
                }

                Func<CancellationToken, ValueTask<int>> getTotalCount =
                    ct => throw new InvalidOperationException();

                // TotalCount is one of the heaviest operations. It is only necessary to load totalCount
                // when it is enabled (IncludeTotalCount) and when it is contained in the selection set.
                if (IncludeTotalCount &&
                    context.Field.Type is ObjectType objectType &&
                    context.FieldSelection.SelectionSet is {} selectionSet)
                {
                    IReadOnlyList<IFieldSelection> selections = context
                        .GetSelections(objectType, selectionSet, true);

                    var includeTotalCount = false;
                    for (var i = 0; i < selections.Count; i++)
                    {
                        if (selections[i].Field.Name.Value is "totalCount")
                        {
                            includeTotalCount = true;
                            break;
                        }
                    }

                    // When totalCount is included in the selection set we prefetch it, then capture the
                    // count in a variable, to pass it into the clojure
                    if (includeTotalCount)
                    {
                        var captureCount = await original
                            .CountAsync(context.RequestAborted)
                            .ConfigureAwait(false);
                        getTotalCount = ct => new ValueTask<int>(captureCount);
                    }
                }

                return new CollectionSegment(
                    (IReadOnlyCollection<object>)items,
                    pageInfo,
                    getTotalCount);
            }
        }
    }
}
