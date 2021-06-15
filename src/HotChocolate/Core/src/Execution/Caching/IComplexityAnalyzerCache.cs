using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Pipeline.Complexity;

namespace HotChocolate.Execution.Caching
{
    internal interface IComplexityAnalyzerCache
    {
        /// <summary>
        /// Gets maximum amount of operation complexity analyzers that can be cached. The default
        /// value is <c>100</c>. The minimum allowed value is <c>10</c>.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets the amount of operation analyzers residing in the cache.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Try get a compiled operation complexity analyzer by it <paramref name="operationId" />.
        /// </summary>
        /// <param name="operationId">
        /// The internal operation id.
        /// </param>
        /// <param name="analyzer">
        /// The operation complexity analyzer that is associated with the id or null
        /// if no operation complexity analyzer was found that matches the specified id.
        /// </param>
        /// <returns>
        /// <c>true</c> if an operation complexity analyzer was found that matches the specified
        /// <paramref name="operationId"/>, otherwise <c>false</c>.
        /// </returns>
        bool TryGetOperation(
            string operationId,
            [NotNullWhen(true)] out ComplexityAnalyzerDelegate? analyzer);

        /// <summary>
        /// Tries to add a new operation complexity analyzer to the cache.
        /// </summary>
        /// <param name="operationId">
        /// The internal operation id.
        /// </param>
        /// <param name="analyzer">
        /// The operation complexity analyzer that shall be cached.
        /// </param>
        void TryAddOperation(
            string operationId,
            ComplexityAnalyzerDelegate analyzer);

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        void Clear();
    }
}
