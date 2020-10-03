using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    /// <summary>
    /// This remote executor delegates GraphQL query, mutation and subscription requests for the
    /// remote <see cref="IRequestExecutor.Schema" /> to the GraphQL server that can process them.
    /// </summary>
    public interface IRemoteRequestExecutor
    {
        /// <summary>
        /// Gets the schema to which this executor is bound to.
        /// </summary>
        ISchema Schema { get; }

        /// <summary>
        /// Gets the services that are bound to this executor.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Executes the given GraphQL <paramref name="request" />.
        /// </summary>
        /// <param name="request">
        /// The GraphQL request object.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// Returns the execution result of the given GraphQL <paramref name="request" />.
        ///
        /// If the request operation is a simple query or mutation the result is a
        /// <see cref="IQueryResult" />.
        ///
        /// If the request operation is a query or mutation where data is deferred, streamed or
        /// includes live data the result is a <see cref="IResponseStream" /> where each result
        /// that the <see cref="IResponseStream" /> yields is a <see cref="IQueryResult" />.
        ///
        /// If the request operation is a subscription the result is a
        /// <see cref="IResponseStream" /> where each result that the
        /// <see cref="IResponseStream" /> yields is a
        /// <see cref="IReadOnlyQueryResult" />.
        /// </returns>
        Task<IExecutionResult> ExecuteAsync(
            IQueryRequest request,
            CancellationToken cancellationToken = default);
    }
}
