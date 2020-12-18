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
    public class MongoDbCursorPagingProvider : CursorPagingProvider
    {
        private static readonly MethodInfo _createHandler =
            typeof(MongoDbCursorPagingProvider).GetMethod(
                nameof(CreateHandlerInternal),
                BindingFlags.Static | BindingFlags.NonPublic)!;

        public override bool CanHandle(IExtendedType source)
        {
            return source.Source == typeof(IExecutable) ||
                typeof(IMongoDbExecutable).IsAssignableFrom(source.Source) ||
                source.Source.IsGenericTypeDefinition &&
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
                .Invoke(null, new object[] { options })!;
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
                return ResolveAsync(f, arguments, context.RequestAborted);
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

            private async ValueTask<Connection> ResolveAsync(
                IMongoPagingContainer<TEntity> source,
                CursorPagingArguments arguments = default,
                CancellationToken cancellationToken = default)
            {
                var count = await source.CountAsync(cancellationToken).ConfigureAwait(false);

                int? after = arguments.After is { } a
                    ? (int?)IndexEdge<TEntity>.DeserializeCursor(a)
                    : null;

                int? before = arguments.Before is { } b
                    ? (int?)IndexEdge<TEntity>.DeserializeCursor(b)
                    : null;

                IReadOnlyList<IndexEdge<TEntity>> selectedEdges =
                    await GetSelectedEdgesAsync(
                            source,
                            arguments.First,
                            arguments.Last,
                            after,
                            before,
                            cancellationToken)
                        .ConfigureAwait(false);

                IndexEdge<TEntity>? firstEdge = selectedEdges.Count == 0
                    ? null
                    : selectedEdges[0];

                IndexEdge<TEntity>? lastEdge = selectedEdges.Count == 0
                    ? null
                    : selectedEdges[selectedEdges.Count - 1];

                var pageInfo = new ConnectionPageInfo(
                    lastEdge?.Index < count - 1,
                    firstEdge?.Index > 0,
                    firstEdge?.Cursor,
                    lastEdge?.Cursor,
                    count);

                return new Connection<TEntity>(
                    selectedEdges,
                    pageInfo,
                    ct => new ValueTask<int>(pageInfo.TotalCount ?? 0));
            }

            private async ValueTask<IReadOnlyList<IndexEdge<TEntity>>> GetSelectedEdgesAsync(
                IMongoPagingContainer<TEntity> allEdges,
                int? first,
                int? last,
                int? after,
                int? before,
                CancellationToken cancellationToken)
            {
                var (offset, edges) =
                    await GetEdgesToReturnAsync(
                            allEdges,
                            first,
                            last,
                            after,
                            before,
                            cancellationToken)
                        .ConfigureAwait(false);

                return await ExecuteQueryableAsync(edges, offset, cancellationToken);
            }

            private async Task<(int, IMongoPagingContainer<TEntity>)> GetEdgesToReturnAsync(
                IMongoPagingContainer<TEntity> allEdges,
                int? first,
                int? last,
                int? after,
                int? before,
                CancellationToken cancellationToken)
            {
                IMongoPagingContainer<TEntity> edges = ApplyCursorToEdges(allEdges, after, before);

                var offset = 0;
                if (after.HasValue)
                {
                    offset = after.Value + 1;
                }

                if (first.HasValue)
                {
                    edges = GetFirstEdges(edges, first.Value);
                }

                if (last.HasValue)
                {
                    var (newOffset, newEdges) =
                        await GetLastEdgesAsync(edges, last.Value, offset, cancellationToken)
                            .ConfigureAwait(false);

                    edges = newEdges;
                    offset = newOffset;
                }

                return (offset, edges);
            }

            protected virtual IMongoPagingContainer<TEntity> GetFirstEdges(
                IMongoPagingContainer<TEntity> edges,
                int first)
            {
                if (first < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(first));
                }

                return edges.Take(first);
            }

            protected virtual Task<(int, IMongoPagingContainer<TEntity>)> GetLastEdgesAsync(
                IMongoPagingContainer<TEntity> edges,
                int last,
                int offset,
                CancellationToken cancellationToken)
            {
                if (last < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(last));
                }

                return GetLastEdgesAsyncInternal(edges, last, offset, cancellationToken);
            }

            private async Task<(int, IMongoPagingContainer<TEntity>)>
                GetLastEdgesAsyncInternal(
                    IMongoPagingContainer<TEntity> edges,
                    int last,
                    int offset,
                    CancellationToken cancellationToken)
            {
                IMongoPagingContainer<TEntity> temp = edges;

                var count = await edges.CountAsync(cancellationToken).ConfigureAwait(false);
                var skip = count - last;

                if (skip > 1)
                {
                    temp = temp.Skip(skip);
                    offset += count;
                    offset -= await edges.CountAsync(cancellationToken).ConfigureAwait(false);
                }

                return (offset, temp);
            }

            protected virtual IMongoPagingContainer<TEntity> ApplyCursorToEdges(
                IMongoPagingContainer<TEntity> allEdges,
                int? after,
                int? before)
            {
                IMongoPagingContainer<TEntity> edges = allEdges;

                if (after.HasValue)
                {
                    edges = edges.Skip(after.Value + 1);
                }

                if (before.HasValue)
                {
                    edges = edges.Take(before.Value);
                }

                return edges;
            }

            protected virtual async ValueTask<IReadOnlyList<IndexEdge<TEntity>>>
                ExecuteQueryableAsync(
                    IMongoPagingContainer<TEntity> container,
                    int offset,
                    CancellationToken cancellationToken)
            {
                return await container.ToIndexEdgesAsync(offset, cancellationToken);
            }
        }
    }
}
