using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Client
{
    /// <summary>
    /// Defines a connection for making requests against a GraphQL API endpoint.
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Runs the specified GraphQL query as an asynchronous operation.
        /// </summary>
        /// <param name="query">The GraphQL query to run.</param>
        /// <param name="cancellationToken">
        /// The cancellation token to use.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous
        /// operation that returns the result of the GraphQL query.
        /// </returns>
        Task<string> Run(IQueryRequest request, CancellationToken cancellationToken);
    }

    public interface IQueryRequest
    {
        string OperationName { get; }

        string NamedQuery { get; }

        DocumentNode Query { get; }

        IDictionary<string, object> Variables { get; }
    }



}
