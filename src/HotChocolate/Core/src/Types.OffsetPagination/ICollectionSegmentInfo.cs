using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination
{
    public interface ICollectionSegmentInfo
    {

        bool HasNextPage { get; }

        bool HasPreviousPage { get; }

        /// <summary>
        /// If <see cref="TotalCount"/> is supported by the <see cref="ICollectionSegmentResolver"/>
        /// then this property will provide the total number of entities the current data set
        /// provides.
        /// </summary>
        long? TotalCount { get; }
    }

    public interface ICollectionSegmentResolver
    {
        /// <summary>
        /// Resolves a connection for a pageable data source.
        /// </summary>
        /// <param name="context">
        /// The middleware context.
        /// </param>
        /// <param name="source">
        /// The data source.
        /// </param>
        /// <param name="arguments">
        /// The connection arguments passed in from the query.
        /// </param>
        /// <param name="withTotalCount">
        /// The middleware requested a connection with a total count.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// Returns a connection which represents a page in the result set.
        /// </returns>
        ValueTask<ICollectionSegment> ResolveAsync(
            IMiddlewareContext context,
            object source,
            ConnectionArguments arguments = default,
            bool withTotalCount = false,
            CancellationToken cancellationToken = default);
    }

    public readonly struct ConnectionArguments
    {
        public ConnectionArguments(
            int? first = null,
            int? last = null,
            string? after = null,
            string? before = null)
        {
            First = first;
            Last = last;
            After = after;
            Before = before;
        }

        public int? First { get; }

        public int? Last { get; }

        /// <summary>
        /// The cursor after which entities shall be taken.
        /// </summary>
        public string? After { get; }

        /// <summary>
        /// The cursor before which entities shall be taken.
        /// </summary>
        public string? Before { get; }
    }
}
