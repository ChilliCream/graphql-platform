using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Client
{
    /// <summary>
    /// Defines a connection for making HTTP requests against the GitHub GraphQL API endpoint.
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Gets the base URI for the connection.
        /// </summary>
        Uri Uri { get; }

        /// <summary>
        /// Runs the specified GraphQL query as an asynchronous operation.
        /// </summary>
        /// <param name="query">The GraphQL query to run.</param>
        /// <param name="cancellationToken">The optional cancellation token to use.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation that returns the result of the GraphQL query.
        /// </returns>
        Task<string> Run(string query, CancellationToken cancellationToken = default);
    }
}
