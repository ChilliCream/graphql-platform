using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Paging
{
    public class QueryableConnectionResolver<T>
        : IConnectionResolver
    {
        private const string _totalCount = "__totalCount";
        private const string _position = "__position";

        private readonly IQueryable<T> _source;
        private readonly IDictionary<string, object> _properties;
        private readonly QueryablePagingDetails _pageDetails;

        public QueryableConnectionResolver(
            IQueryable<T> source,
            PagingDetails pagingDetails)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (pagingDetails == null)
            {
                throw new ArgumentNullException(nameof(pagingDetails));
            }

            _source = source;
            _pageDetails = DeserializePagingDetails(pagingDetails);

            _properties = pagingDetails.Properties
                ?? new Dictionary<string, object>();
        }

        public Task<Connection<T>> ResolveAsync(
            CancellationToken cancellationToken)
        {
            return Task.Run(() => Create(), cancellationToken);
        }

        private Connection<T> Create()
        {
            IQueryable<T> edges = ApplyCursorToEdges(
                _source, _pageDetails.Before, _pageDetails.After);

            if (!_pageDetails.TotalCount.HasValue)
            {
                _pageDetails.TotalCount = _source.Count();
                _properties[_totalCount] =
                    _pageDetails.TotalCount.Value;
            }

            IReadOnlyCollection<QueryableEdge> selectedEdges =
                GetSelectedEdges();
            QueryableEdge firstEdge = selectedEdges.FirstOrDefault();
            QueryableEdge lastEdge = selectedEdges.LastOrDefault();


            var pageInfo = new PageInfo(
                lastEdge?.Index < (_pageDetails.TotalCount.Value - 1),
                firstEdge?.Index > 0,
                selectedEdges.FirstOrDefault()?.Cursor,
                selectedEdges.LastOrDefault()?.Cursor);

            return new Connection<T>(pageInfo, selectedEdges);
        }

        Task<IConnection> IConnectionResolver.ResolveAsync(
            CancellationToken cancellationToken) =>
                Task.Run<IConnection>(() => Create(), cancellationToken);

        private IReadOnlyCollection<QueryableEdge> GetSelectedEdges()
        {
            List<QueryableEdge> list = new List<QueryableEdge>();
            List<T> edges = GetEdgesToReturn(
                _source, _pageDetails, out int offset);

            for (int i = 0; i < edges.Count; i++)
            {
                int index = offset + i;
                _properties[_position] = index;
                string cursor = Base64Serializer.Serialize(_properties);
                list.Add(new QueryableEdge(cursor, edges[i], index));
            }

            return list;
        }

        private List<T> GetEdgesToReturn(
            IQueryable<T> allEdges,
            QueryablePagingDetails pagingDetails,
            out int offset)
        {
            IQueryable<T> edges = ApplyCursorToEdges(
                allEdges, pagingDetails.Before, pagingDetails.After);

            offset = 0;
            if (pagingDetails.After.HasValue)
            {
                offset = pagingDetails.After.Value + 1;
            }

            if (pagingDetails.First.HasValue)
            {
                edges = GetFirstEdges(
                    edges, pagingDetails.First.Value,
                    ref offset);
            }

            if (pagingDetails.Last.HasValue)
            {
                edges = GetLastEdges(
                    edges, pagingDetails.Last.Value,
                    ref offset);
            }

            return edges.ToList();
        }

        protected virtual IQueryable<T> GetFirstEdges(
            IQueryable<T> edges, int first,
            ref int offset)
        {
            if (first < 0)
            {
                throw new ArgumentException();
            }
            return edges.Take(first);
        }

        protected virtual IQueryable<T> GetLastEdges(
            IQueryable<T> edges, int last,
            ref int offset)
        {
            if (last < 0)
            {
                throw new ArgumentException();
            }

            IQueryable<T> temp = edges;

            int count = temp.Count();
            int skip = count - last;

            if (skip > 1)
            {
                temp = temp.Skip(skip);
                offset += count;
                offset -= temp.Count();
            }

            return temp;
        }

        protected virtual IQueryable<T> ApplyCursorToEdges(
            IQueryable<T> allEdges, int? before, int? after)
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

        private static QueryablePagingDetails DeserializePagingDetails(
            PagingDetails pagingDetails)
        {
            Dictionary<string, object> afterProperties =
                DeserializeCursor(pagingDetails.After);
            Dictionary<string, object> beforeProperties =
                DeserializeCursor(pagingDetails.Before);

            return new QueryablePagingDetails
            {
                After = GetPositionFromCurser(afterProperties),
                Before = GetPositionFromCurser(beforeProperties),
                TotalCount = GetPositionFromCurser(afterProperties)
                    ?? GetPositionFromCurser(beforeProperties),
                First = pagingDetails.First,
                Last = pagingDetails.Last
            };
        }

        private static int? GetPositionFromCurser(
            Dictionary<string, object> properties)
        {
            if (properties == null)
            {
                return null;
            }

            return Convert.ToInt32(properties[_position]);
        }

        private static int? GetTotalCountFromCursor(
            Dictionary<string, object> properties)
        {
            if (properties == null)
            {
                return null;
            }

            return Convert.ToInt32(properties[_totalCount]);
        }

        private static Dictionary<string, object> DeserializeCursor(
            string cursor)
        {
            if (cursor == null)
            {
                return null;
            }

            var properties = Base64Serializer
                .Deserialize<Dictionary<string, object>>(cursor);

            return properties;
        }

        protected class QueryablePagingDetails
        {
            public int? TotalCount { get; set; }
            public int? Before { get; set; }
            public int? After { get; set; }
            public int? First { get; set; }
            public int? Last { get; set; }
        }

        protected class QueryableEdge
            : Edge<T>
        {
            public QueryableEdge(string cursor, T node, int index)
                : base(cursor, node)
            {
                Index = index;
            }

            public int Index { get; set; }
        }
    }
}
