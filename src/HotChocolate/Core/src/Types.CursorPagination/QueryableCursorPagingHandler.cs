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
            CancellationToken ct = context.RequestAborted;
            return source switch
            {
                IQueryable<TEntity> q => ResolveAsync(q, arguments, ct),
                IEnumerable<TEntity> e => ResolveAsync(e.AsQueryable(), arguments, ct),
                IExecutable<TEntity> ex => SliceAsync(context, ex.Source, arguments),
                _ => throw new GraphQLException("Cannot handle the specified data source.")
            };
        }

        private async ValueTask<Connection> ResolveAsync(
            IQueryable<TEntity> source,
            CursorPagingArguments arguments = default,
            CancellationToken cancellationToken = default)
        {
            var count = await Task.Run(source.Count, cancellationToken)
                .ConfigureAwait(false);

            int? after = arguments.After is { } a
                ? (int?)IndexEdge<TEntity>.DeserializeCursor(a)
                : null;

            int? before = arguments.Before is { } b
                ? (int?)IndexEdge<TEntity>.DeserializeCursor(b)
                : null;

            IReadOnlyList<IndexEdge<TEntity>> selectedEdges =
                await GetSelectedEdgesAsync(
                    source, arguments.First, arguments.Last, after, before, cancellationToken)
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
            IQueryable<TEntity> allEdges,
            int? first,
            int? last,
            int? after,
            int? before,
            CancellationToken cancellationToken)
        {
            IQueryable<TEntity> edges = GetEdgesToReturn(
                allEdges, first, last, after, before,
                out var offset);

            return await ExecuteQueryableAsync(edges, offset, cancellationToken)
                .ConfigureAwait(false);
        }

        private IQueryable<TEntity> GetEdgesToReturn(
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

        protected virtual IQueryable<TEntity> GetFirstEdges(
            IQueryable<TEntity> edges, int first,
            ref int offset)
        {
            if (first < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(first));
            }
            return edges.Take(first);
        }

        protected virtual IQueryable<TEntity> GetLastEdges(
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

            if (skip > 0)
            {
                temp = temp.Skip(skip);
                offset += count;
                offset -= temp.Count();
            }

            return temp;
        }

        protected virtual IQueryable<TEntity> ApplyCursorToEdges(
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

        protected virtual async ValueTask<IReadOnlyList<IndexEdge<TEntity>>> ExecuteQueryableAsync(
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
                await Task.Run(() =>
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
    }
}
