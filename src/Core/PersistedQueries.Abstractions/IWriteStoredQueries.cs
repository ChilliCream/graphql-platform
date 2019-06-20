using HotChocolate.Execution;
using System.Threading.Tasks;

namespace HotChocolate.PersistedQueries.Abstractions
{
    /// <summary>
    /// A tool for storing queries to some persistence medium.
    /// </summary>
    public interface IStoreQueries
    {
        /// <summary>
        /// Stores a given query using the given identifier.
        /// </summary>
        /// <param name="queryIdentifier">The query identifier.</param>
        /// <param name="query">The query to store.</param>
        /// <returns>An asynchronous operation.</returns>
        Task StoreQueryAsync(string queryIdentifier, IQuery query);
    }
}
