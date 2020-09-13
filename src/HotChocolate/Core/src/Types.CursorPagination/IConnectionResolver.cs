using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination
{
    public interface IConnectionResolver
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
        ValueTask<IConnection> ResolveAsync(
            IMiddlewareContext context,
            object source,
            ConnectionArguments arguments = default,
            bool withTotalCount = false,
            CancellationToken cancellationToken = default);
    }

    public class CursorPagingProvider<TSource> : IPagingProvider<>
    {
        public ValueTask<Connection> SliceAsync(
            CursorPagingContext<TSource> context,
            CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        async ValueTask<IPage> IPagingProvider<>.SliceAsync(
            IPagingContext context,
            CancellationToken cancellationToken) =>
            await SliceAsync((CursorPagingContext<TSource>)context, cancellationToken);

        IPagingContext IPagingProvider<>.CreateContext(
            IResolverContext context,
            object source)
        {

        }

    }

    public class CursorPagingContext<TSource>
        : IPagingContext
    {
        public IResolverContext ResolverContext { get; }
        public object Source { get; }
        public bool IncludeTotalCount { get; }
    }
}
