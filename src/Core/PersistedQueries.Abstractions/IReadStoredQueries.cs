using HotChocolate.Execution;
using System.Threading.Tasks;

namespace HotChocolate.PersistedQueries.Abstractions
{
    /// <summary>
    /// A tool for reading queries from some persistence medium.
    /// </summary>
    public interface IReadStoredQueries
    {
        /// <summary>
        /// Retrieves the query associated with the given identifier.
        /// </summary>
        /// <param name="queryIdentifier">The query identifier.</param>
        /// <returns>The desired query.</returns>
        Task<IQuery> ReadQueryAsync(string queryIdentifier);
    }
}