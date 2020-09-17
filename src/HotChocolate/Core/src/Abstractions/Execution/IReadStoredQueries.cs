using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution
{
    /// <summary>
    /// A tool for reading queries from some persistence medium.
    /// </summary>
    public interface IReadStoredQueries
    {
        /// <summary>
        /// Retrieves the query associated with the given identifier.
        /// If the query is not found <c>null</c> is returned.
        /// </summary>
        /// <param name="queryId">The query identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The desired query or null if no query
        /// is found with the specified identifier.
        /// </returns>
        Task<QueryDocument?> TryReadQueryAsync(
            string queryId,
            CancellationToken cancellationToken = default);
    }
}
