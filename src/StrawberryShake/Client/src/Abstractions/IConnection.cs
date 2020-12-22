using System.Collections.Generic;
using System.Threading;

namespace StrawberryShake
{
    /// <summary>
    /// A connection represents a transport connection to a GraphQL server and allows to execute
    /// requests against it.
    /// </summary>
    /// <typeparam name="TBody"></typeparam>
    public interface IConnection<TBody> where TBody : class
    {
        /// <summary>
        /// Executes a request and yields the results.
        /// </summary>
        /// <param name="request">
        /// The operation request that shall be send to the server.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The results of the request.
        /// </returns>
        IAsyncEnumerable<Response<TBody>> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default);
    }
}
