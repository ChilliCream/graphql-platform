using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution
{
    /// <summary>
    /// A tool for storing queries to some persistence medium.
    /// </summary>
    public interface IWriteStoredQueries
    {
        /// <summary>
        /// Stores a given query using the given identifier.
        /// </summary>
        /// <param name="queryId">The query identifier.</param>
        /// <param name="query">The query to store.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An asynchronous operation.</returns>
        Task WriteQueryAsync(
            string queryId,
            IQuery query,
            CancellationToken cancellationToken = default);
    }
}
