using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Paging
{
    /// <summary>
    /// An offset paging provider for MongoDb that create pagination queries
    /// </summary>
    public class MongoDbOffsetPagingProvider : OffsetPagingProvider
    {
        private static readonly MethodInfo _createHandler =
            typeof(MongoDbOffsetPagingProvider).GetMethod(
                nameof(CreateHandlerInternal),
                BindingFlags.Static | BindingFlags.NonPublic)!;

        public override bool CanHandle(IExtendedType source)
        {
            return typeof(IMongoDbExecutable).IsAssignableFrom(source.Source) ||
                source.Source.IsGenericType &&
                source.Source.GetGenericTypeDefinition() is { } type && (
                    type == typeof(IAggregateFluent<>) ||
                    type == typeof(IFindFluent<,>) ||
                    type == typeof(IMongoCollection<>));
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
            PagingOptions options) => new(options);

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
                return OffsetPagingHelper.ApplyPagination(
                    f,
                    arguments,
                    ApplySkip,
                    ApplyTake,
                    Execute,
                    CountAsync,
                    context.RequestAborted);
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

            private static async ValueTask<IReadOnlyList<TEntity>> Execute(
                IMongoPagingContainer<TEntity> source,
                CancellationToken cancellationToken)
            {
                return await source.ToListAsync(cancellationToken);
            }

            private static IMongoPagingContainer<TEntity> ApplySkip(
                IMongoPagingContainer<TEntity> source,
                int skip) => source.Skip(skip);


            private static IMongoPagingContainer<TEntity> ApplyTake(
                IMongoPagingContainer<TEntity> source,
                int take) => source.Take(take);

            private static async ValueTask<int> CountAsync(
                IMongoPagingContainer<TEntity> source,
                CancellationToken cancellationToken) =>
                await source.CountAsync(cancellationToken);
        }
    }
}
