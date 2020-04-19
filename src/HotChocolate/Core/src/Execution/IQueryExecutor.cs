using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    /// <summary>
    /// This executor processes GraphQL query, mutation and subscription requests for the
    /// <see cref="Schema" /> to which it is bound to.
    /// </summary>
    public interface IQueryExecutor
        : IDisposable
    {
        /// <summary>
        /// Gets the schema to which this executor is bound to.
        /// </summary>
        /// <value></value>
        ISchema Schema { get; }

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
        /// If the request operation is a simple query or mutation the result is a
        /// <see cref="IReadOnlyQueryResult" />.
        ///
        /// If the request operation is a query or mutation where data is deferred, streamed or
        /// includes live data the result is a <see cref="IResponseStream" /> where reach result
        /// that the <see cref="IResponseStream" /> yields is a <see cref="IReadOnlyQueryResult" />.
        ///
        /// If the request operation is a subscription the result is a
        /// <see cref="IResponseStream" /> where reach result that the
        /// <see cref="IResponseStream" /> yields is a
        /// <see cref="IReadOnlyQueryResult" />.
        /// </returns>
        Task<IExecutionResult> ExecuteAsync(
            IReadOnlyQueryRequest request,
            CancellationToken cancellationToken = default);
    }
}
