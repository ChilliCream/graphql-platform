using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Relay
{
    public interface IConnectionResolver<T> : IConnectionResolver
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
            T source,
            ConnectionArguments arguments = default,
            bool withTotalCount = false,
            CancellationToken cancellationToken = default);
    }
}
