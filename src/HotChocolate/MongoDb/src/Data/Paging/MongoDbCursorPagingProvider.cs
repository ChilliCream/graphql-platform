using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Paging
{
    public class MongoDbCursorPagingProvider : CursorPagingProvider
    {
        private static readonly MethodInfo _createHandler =
            typeof(MongoDbCursorPagingProvider).GetMethod(
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

        protected override CursorPagingHandler CreateHandler(
            IExtendedType source,
            PagingOptions options)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return (CursorPagingHandler)_createHandler
                .MakeGenericMethod(source.ElementType?.Source ?? source.Source)
                .Invoke(null,
                    new object[]
                    {
                        options
                    })!;
        }

        private static MongoDbCursorPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
            PagingOptions options) =>
            new MongoDbCursorPagingHandler<TEntity>(options);


        private class MongoDbCursorPagingHandler<TEntity> : CursorPagingHandler
        {
            public MongoDbCursorPagingHandler(PagingOptions options) : base(options)
            {
            }

            protected override ValueTask<Connection> SliceAsync(
                IResolverContext context,
                object source,
                CursorPagingArguments arguments)
            {
                IMongoPagingContainer<TEntity> f = CreatePagingContainer(source);
                return CursorPagingHelper.ApplyPagination(
                    f,
                    arguments,
                    ApplySkip,
                    ApplyTake,
                    ToIndexEdgesAsync,
                    CountAsync,
                    context.RequestAborted);
            }

            private static ValueTask<IReadOnlyList<IndexEdge<TEntity>>>
                ToIndexEdgesAsync(
                IMongoPagingContainer<TEntity> source,
                int offset,
                CancellationToken cancellationToken)
            {
                return source.ToIndexEdgesAsync(offset, cancellationToken);
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
        }
    }

}
