using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Caching
{
    /// <summary>
    /// This cache is used to cache compiled operations for faster execution and less memory.
    /// </summary>
    public interface IPreparedOperationCache
    {
        /// <summary>
        /// Gets maximum amount of compiled operations that can be cached. The default
        /// value is <c>100</c>. The minimum allowed value is <c>10</c>.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets the amount of compiled queries residing in the cache.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Try get a compiled operation by it <paramref name="operationId" />.
        /// </summary>
        /// <param name="operationId">
        /// The internal operation id.
        /// </param>
        /// <param name="operation">
        /// The operation that is associated with the id or null if no operation was found
        /// that matches the specified id.
        /// </param>
        /// <returns>
        /// <c>true</c> if an operation was found that matches the specified
        /// <paramref name="operationId"/>, otherwise <c>false</c>.
        /// </returns>
        bool TryGetOperation(
            string operationId,
            [NotNullWhen(true)] out IPreparedOperation? operation);

        /// <summary>
        /// Tries to add a new compiled operation to the cache.
        /// </summary>
        /// <param name="operationId">
        /// The internal operation id.
        /// </param>
        /// <param name="operation">
        /// The operation that shall be cached.
        /// </param>
        void TryAddOperation(
            string operationId,
            IPreparedOperation operation);

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        void Clear();
    }
}
