using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Paging
{
    public class QueryableConnectionFactory<T>
        : IConnectionFactory
    {
        private const string _position = "__position";

        private readonly IQueryable<T> _source;
        private readonly IDictionary<string, object> _properties;
        private readonly QueryablePagingDetails _pageDetails;
        private readonly bool _hasNextRequested;
        private readonly bool _hasPreviousRequested;

        public QueryableConnectionFactory(
            IQueryable<T> source,
            PagingDetails pagingDetails,
            bool hasNextRequested,
            bool hasPreviousRequested)
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
            _hasNextRequested = hasNextRequested;
            _hasPreviousRequested = hasPreviousRequested;

            _properties = pagingDetails.Properties
                ?? new Dictionary<string, object>();
        }

        public Task<Connection<T>> CreateAsync(
            CancellationToken cancellationToken)
        {
            return Task.Run(() => Create(), cancellationToken);
        }

        private Connection<T> Create()
        {
            IQueryable<T> edges = ApplyCursorToEdges(
                _source, _pageDetails.Before, _pageDetails.After);

            bool hasNextPage = _hasNextRequested
                && HasNextPage(edges, _pageDetails);
            bool hasPreviousPage = _hasPreviousRequested
                && HasPreviousPage(edges, _pageDetails);

            IReadOnlyCollection<Edge<T>> selectedEdges = GetSelectedEdges();

            var pageInfo = new PageInfo(
                hasNextPage, hasPreviousPage,
                selectedEdges.FirstOrDefault()?.Cursor,
                selectedEdges.LastOrDefault()?.Cursor);

            return new Connection<T>(pageInfo, selectedEdges);
        }

        Task<IConnection> IConnectionFactory.CreateAsync(
            CancellationToken cancellationToken) =>
                Task.Run<IConnection>(() => Create(), cancellationToken);

        public IReadOnlyCollection<Edge<T>> GetSelectedEdges()
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

        protected virtual bool HasNextPage(
            IQueryable<T> appliedEdges,
            QueryablePagingDetails pagingDetails)
        {
            if (pagingDetails.First.HasValue)
            {
                return appliedEdges.Skip(pagingDetails.First.Value).Any();
            }

            if (pagingDetails.Before.HasValue)
            {
                return appliedEdges.Any();
            }

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

    public class QueryableConnectionMiddleware<T>
    {
        private readonly FieldDelegate _next;

        public QueryableConnectionMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context);

            var pagingDetails = new PagingDetails
            {
                First = context.Argument<int?>("first"),
                After = context.Argument<string>("after"),
                Last = context.Argument<int?>("last"),
                Before = context.Argument<string>("before"),
                // TODO : how do we get the Properties down here?
            };

            IQueryable<T> source = null;

            if (context.Result is IQueryable<T> q)
            {
                source = q;
            }
            if (context.Result is IEnumerable<T> e)
            {
                source = e.AsQueryable();
            }

            if (source != null)
            {
                var connectionFactory = new QueryableConnectionFactory<T>(
                        source,
                        pagingDetails,
                        HasNextPageRequested(context.FieldSelection),
                        HasPreviousPageRequested(context.FieldSelection));
            }
        }

        private bool HasNextPageRequested(FieldNode fieldSelection) =>
            IsPageInfoFieldSelected(fieldSelection, "hasNextPage");

        private bool HasPreviousPageRequested(FieldNode fieldSelection) =>
            IsPageInfoFieldSelected(fieldSelection, "hasPreviousPage");


        private bool IsPageInfoFieldSelected(
            FieldNode fieldSelection,
            string fieldName)
        {
            if (fieldSelection.SelectionSet == null)
            {
                return false;
            }

            FieldNode pageInfo = fieldSelection.SelectionSet.Selections
                .OfType<FieldNode>().FirstOrDefault(
                    t => t.Name.Value.EqualsOrdinal("pageInfo"));

            if (pageInfo == null)
            {
                return false;
            }

            return fieldSelection.SelectionSet.Selections
                .OfType<FieldNode>().Any(
                    t => t.Name.Value.EqualsOrdinal(fieldName));
        }
    }
}
