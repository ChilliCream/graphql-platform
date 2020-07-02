using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

#nullable  enable

namespace HotChocolate.Types.Relay
{
    public class QueryableConnectionResolver<T>
        : IConnectionResolver<IQueryable<T>>
    {
        public async ValueTask<IConnection> ResolveAsync(
            IMiddlewareContext context,
            IQueryable<T> source,
            ConnectionArguments arguments = default,
            bool withTotalCount = false,
            CancellationToken cancellationToken = default)
        {
            var count = await Task.Run(source.Count, cancellationToken)
                .ConfigureAwait(false);

            int? after = arguments.After is { } a
                ? (int?)IndexEdge<T>.DeserializeCursor(a)
                : null;

            int? before = arguments.Before is { } b
                ? (int?)IndexEdge<T>.DeserializeCursor(b)
                : null;

            List<IndexEdge<T>> selectedEdges =
                await GetSelectedEdgesAsync(
                    source, arguments.First, arguments.Last, after, before, cancellationToken)
                    .ConfigureAwait(false);

            IndexEdge<T>? firstEdge = selectedEdges.Count == 0 ? null : selectedEdges[0];
            IndexEdge<T>? lastEdge = selectedEdges.Count == 0 ? null : selectedEdges[selectedEdges.Count - 1];

            var pageInfo = new PageInfo(
                lastEdge?.Index < count - 1,
                firstEdge?.Index > 0,
                firstEdge?.Cursor,
                lastEdge?.Cursor,
                count);

            return new Connection<T>(pageInfo, selectedEdges);
        }

        ValueTask<IConnection> IConnectionResolver.ResolveAsync(
            IMiddlewareContext context,
            object source,
            ConnectionArguments arguments,
            bool withTotalCount,
            CancellationToken cancellationToken) =>
            ResolveAsync(
                context,
                (IQueryable<T>)source,
                arguments,
                withTotalCount,
                cancellationToken);

        private async ValueTask<List<IndexEdge<T>>> GetSelectedEdgesAsync(
            IQueryable<T> allEdges,
            int? first,
            int? last,
            int? after,
            int? before,
            CancellationToken cancellationToken)
        {
            IQueryable<T> edges = GetEdgesToReturn(
                allEdges, first, last, after, before,
                out var offset);

            var list = new List<IndexEdge<T>>();

            if (edges is IAsyncEnumerable<T> enumerable)
            {
                var index = offset;
                await foreach (T item in enumerable.WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    list.Add(IndexEdge<T>.Create(item, index++));
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    var index = offset;
                    foreach (T item in edges)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        list.Add(IndexEdge<T>.Create(item, index++));
                    }
                }).ConfigureAwait(false);
            }

            return list;
        }

        private IQueryable<T> GetEdgesToReturn(
            IQueryable<T> allEdges,
            int? first,
            int? last,
            int? after,
            int? before,
            out int offset)
        {
            IQueryable<T> edges = ApplyCursorToEdges(allEdges, after, before);

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

        protected virtual IQueryable<T> GetFirstEdges(
            IQueryable<T> edges, int first,
            ref int offset)
        {
            if (first < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(first));
            }
            return edges.Take(first);
        }

        protected virtual IQueryable<T> GetLastEdges(
            IQueryable<T> edges,
            int last,
            ref int offset)
        {
            if (last < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(last));
            }

            IQueryable<T> temp = edges;

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

        protected virtual IQueryable<T> ApplyCursorToEdges(
            IQueryable<T> allEdges,
            int? after,
            int? before)
        {
            IQueryable<T> edges = allEdges;

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
