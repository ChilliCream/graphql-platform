using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing.Plan;

namespace HotChocolate.Execution.Caching
{
    /// <summary>
    /// This cache is used to cache compiled operations for faster execution and less memory.
    /// </summary>
    internal interface IQueryPlanCache
    {
        /// <summary>
        /// Gets maximum amount of query plans that can be cached. The default
        /// value is <c>100</c>. The minimum allowed value is <c>10</c>.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets the amount of compiled query plans residing in the cache.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Try get a query plan by its <paramref name="operationId" />.
        /// </summary>
        /// <param name="operationId">
        /// The internal operation id.
        /// </param>
        /// <param name="queryPlan">
        /// The query plan that is associated with the id or null if no query plan was found
        /// that matches the specified id.
        /// </param>
        /// <returns>
        /// <c>true</c> if an query plans was found that matches the specified
        /// <paramref name="operationId"/>, otherwise <c>false</c>.
        /// </returns>
        bool TryGetQueryPlan(string operationId, [NotNullWhen(true)] out QueryPlan? queryPlan);

        /// <summary>
        /// Tries to add a new query plan to the cache.
        /// </summary>
        /// <param name="operationId">
        /// The internal operation id.
        /// </param>
        /// <param name="queryPlan">
        /// The query plan that shall be cached.
        /// </param>
        void TryAddQueryPlan(string operationId, QueryPlan queryPlan);

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        void Clear();
    }
}
