using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination
{
    public class QueryableCursorPagingHandler<TEntity> : CursorPagingHandler
    {
        public QueryableCursorPagingHandler(PagingOptions options)
            : base(options)
        {
        }

        protected override ValueTask<Connection> SliceAsync(
            IResolverContext context,
            object source,
            CursorPagingArguments arguments)
        {
            if (source is IQueryable<TEntity> queryable)
            {
                return ResolveAsync(queryable, arguments, context.RequestAborted);
            }

            if (source is IEnumerable<TEntity> enumerable)
            {
                return ResolveAsync(enumerable.AsQueryable(), arguments, context.RequestAborted);
            }

            throw new GraphQLException("Cannot handle the specified data source.");
        }

        private async ValueTask<Connection> ResolveAsync(
            IQueryable<TEntity> source,
            CursorPagingArguments arguments = default,
            CancellationToken cancellationToken = default)
        {
            var count = await Task.Run(source.Count, cancellationToken)
                .ConfigureAwait(false);

            IQueryable<TEntity> edges = SliceSource(source, arguments, out var offset);

            IReadOnlyList<IndexEdge<TEntity>> selectedEdges =
                await ExecuteQueryableAsync(edges, offset, cancellationToken)
                    .ConfigureAwait(false);

            return CreateConnection(selectedEdges, count);
        }

        protected virtual ValueTask<IReadOnlyList<IndexEdge<TEntity>>> ExecuteQueryableAsync(
            IQueryable<TEntity> queryable,
            int offset,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(queryable, offset, cancellationToken);
        }

        public static async ValueTask<IReadOnlyList<IndexEdge<TEntity>>> ExecuteAsync(
            IQueryable<TEntity> queryable,
            int offset,
            CancellationToken cancellationToken)
        {
            var list = new List<IndexEdge<TEntity>>();

            if (queryable is IAsyncEnumerable<TEntity> enumerable)
            {
                var index = offset;
                await foreach (TEntity item in enumerable.WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    list.Add(IndexEdge<TEntity>.Create(item, index++));
                }
            }
            else
            {
                await Task.Run(
                        () =>
                        {
                            var index = offset;
                            foreach (TEntity item in queryable)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    break;
                                }

                                list.Add(IndexEdge<TEntity>.Create(item, index++));
                            }
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return list;
        }

        public static Connection CreateConnection(
            IReadOnlyList<IndexEdge<TEntity>> selectedEdges,
            int count)
        {
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


        public static IQueryable<TEntity> SliceSource(
            IQueryable<TEntity> source,
            CursorPagingArguments arguments,
            out int offset)
        {
            int? after = arguments.After is { } a
                ? (int?)IndexEdge<TEntity>.DeserializeCursor(a)
                : null;

            int? before = arguments.Before is { } b
                ? (int?)IndexEdge<TEntity>.DeserializeCursor(b)
                : null;

            IQueryable<TEntity> edges = GetEdgesToReturn(
                source,
                arguments.First,
                arguments.Last,
                after,
                before,
                out offset);
            return edges;
        }

        private static IQueryable<TEntity> GetEdgesToReturn(
            IQueryable<TEntity> allEdges,
            int? first,
            int? last,
            int? after,
            int? before,
            out int offset)
        {
            IQueryable<TEntity> edges = ApplyCursorToEdges(allEdges, after, before);

            offset = 0;
            if (after.HasValue)
            {
                offset = after.Value + 1;
            }

            if (first.HasValue)
            {
                edges = GetFirstEdges(edges, first.Value, ref offset);
            }

            if (last.HasValue)
            {
                edges = GetLastEdges(edges, last.Value, ref offset);
            }

            return edges;
        }

        private static IQueryable<TEntity> GetFirstEdges(
            IQueryable<TEntity> edges,
            int first,
            ref int offset)
        {
            if (first < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(first));
            }

            return edges.Take(first);
        }

        private static IQueryable<TEntity> GetLastEdges(
            IQueryable<TEntity> edges,
            int last,
            ref int offset)
        {
            if (last < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(last));
            }

            IQueryable<TEntity> temp = edges;

            var count = temp.Count();
            var skip = count - last;

            if (skip > 1)
            {
                temp = temp.Skip(skip);
                offset += count;
                offset -= temp.Count();
            }

            return temp;
        }

        private static IQueryable<TEntity> ApplyCursorToEdges(
            IQueryable<TEntity> allEdges,
            int? after,
            int? before)
        {
            IQueryable<TEntity> edges = allEdges;

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
    }
}
