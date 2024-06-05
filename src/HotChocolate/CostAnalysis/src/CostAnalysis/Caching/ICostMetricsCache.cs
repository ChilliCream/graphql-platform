using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.CostAnalysis.Caching;

internal interface ICostMetricsCache
{
    /// <summary>
    /// Gets the maximum number of <c>CostMetrics</c> instances that can be cached. The default
    /// value is <c>100</c>. The minimum allowed value is <c>10</c>.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// Gets the number of <c>CostMetrics</c> instances residing in the cache.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Tries to get a <c>CostMetrics</c> instance by <paramref name="operationId" />.
    /// </summary>
    /// <param name="operationId">
    /// The internal operation ID.
    /// </param>
    /// <param name="costMetrics">
    /// The <c>CostMetrics</c> instance that is associated with the ID or null
    /// if no <c>CostMetrics</c> instance was found that matches the specified ID.
    /// </param>
    /// <returns>
    /// <c>true</c> if a <c>CostMetrics</c> instance was found that matches the specified
    /// <paramref name="operationId"/>, otherwise <c>false</c>.
    /// </returns>
    bool TryGetCostMetrics(
        string operationId,
        [NotNullWhen(true)] out CostMetrics? costMetrics);

    /// <summary>
    /// Tries to add a new <c>CostMetrics</c> instance to the cache.
    /// </summary>
    /// <param name="operationId">
    /// The internal operation ID.
    /// </param>
    /// <param name="costMetrics">
    /// The <c>CostMetrics</c> instance that shall be cached.
    /// </param>
    void TryAddCostMetrics(
        string operationId,
        CostMetrics costMetrics);

    /// <summary>
    /// Clears all items from the cache.
    /// </summary>
    void Clear();
}
