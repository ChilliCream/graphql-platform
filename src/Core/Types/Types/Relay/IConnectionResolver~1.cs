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
        /// <param name="context">The middleware context.</param>
        /// <param name="source">The data source.</param>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <param name="after">The cursor after which entities shall be taken.</param>
        /// <param name="before">The cursor before which entities shall be taken.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// Returns a connection which represents a page in the result set. 
        /// </returns>
        Task<IConnection> ResolveAsync(
            IMiddlewareContext context,
            T source,
            int? first,
            int? last,
            string? after,
            string? before,
            CancellationToken cancellationToken);
    }
}
