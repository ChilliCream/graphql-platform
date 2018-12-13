using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Paging
{
    public class QueryableConnection<T>
        : IConnection
    {
        private const string _position = "__position";

        private readonly IQueryable<T> _source;
        private readonly IDictionary<string, object> _properties;
        private readonly QueryablePagingDetails _pageDetails;
        private int? _offset;

        public QueryableConnection(
            IQueryable<T> source,
            PagingDetails pagingDetails)
        {
            if (pagingDetails == null)
            {
                throw new ArgumentNullException(nameof(pagingDetails));
            }

            _source = source
                ?? throw new ArgumentNullException(nameof(source));

            _properties = pagingDetails.Properties
                ?? new Dictionary<string, object>();

            _pageDetails = DeserializePagingDetails(pagingDetails);

            PageInfo = new QueryablePageInfo(
                () => HasNextPage(source, _pageDetails),
                () => HasPreviousPage(source, _pageDetails));

        }

        public QueryableConnection(QueryablePageInfo pageInfo)
        {
            this.PageInfo = pageInfo;

        }

        public QueryablePageInfo PageInfo { get; }

        public Task<ICollection<Edge<T>>> GetEdgesAsync(
            CancellationToken cancellationToken)
        {
            return Task.Run<ICollection<Edge<T>>>(() =>
            {
                List<Edge<T>> list = new List<Edge<T>>();
                List<T> edges = GetEdgesToReturn(
                    _source, _pageDetails, out int offset);

                for (int i = 0; i < edges.Count; i++)
                {
                    _properties[_position] = offset + i;
                    string cursor = Base64Serializer.Serialize(_properties);
                    list.Add(new Edge<T>(cursor, edges[i]));
                }
                return list;
            }, cancellationToken);
        }

        IPageInfo IConnection.PageInfo => PageInfo;

        async Task<IEnumerable<IEdge>> IConnection.GetEdgesAsync(
            CancellationToken cancellationToken)
        {
            return await GetEdgesAsync(cancellationToken);
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

            // TODO: this is quite imperformant since it would result in three calls to the source.
            IQueryable<T> temp = edges;
            offset += temp.Count();
            temp = temp
                .Reverse()
                .Take(last)
                .Reverse();
            offset -= temp.Count();
            return temp;
        }

        protected virtual bool HasNextPage(
            IQueryable<T> allEdges,
            QueryablePagingDetails pagingDetails)
        {
            return false;
        }

        protected virtual bool HasPreviousPage(
            IQueryable<T> allEdges,
            QueryablePagingDetails pagingDetails)
        {
            if (pagingDetails.Last.HasValue)
            {
                IQueryable<T> edges = ApplyCursorToEdges(
                    allEdges, pagingDetails.Before, pagingDetails.After);
                return edges.Count() > pagingDetails.Last.Value;
            }

            if (pagingDetails.After.HasValue)
            {
                IQueryable<T> edges = ApplyCursorToEdges(
                    allEdges, null, pagingDetails.After);
                return edges.Any();
            }

            return false;
        }

        private IQueryable<T> ApplyCursorToEdges(
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
            return new QueryablePagingDetails
            {
                After = GetPositionFromCurser(pagingDetails.After),
                Before = GetPositionFromCurser(pagingDetails.Before),
                First = pagingDetails.First,
                Last = pagingDetails.Last
            };
        }

        private static int? GetPositionFromCurser(string cursor)
        {
            if (cursor == null)
            {
                return null;
            }

            var properties = Base64Serializer
                .Deserialize<Dictionary<string, object>>(cursor);

            return Convert.ToInt32(properties[_position]);
        }

        protected class QueryablePagingDetails
        {
            public int? Before { get; set; }
            public int? After { get; set; }
            public int? First { get; set; }
            public int? Last { get; set; }
        }
    }
}
