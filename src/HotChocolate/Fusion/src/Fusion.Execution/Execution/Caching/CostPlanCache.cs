using System.Diagnostics.CodeAnalysis;
using HotChocolate.Caching.Memory;
using HotChocolate.CostAnalysis;

namespace HotChocolate.Fusion.Execution.Caching;

/// <summary>
/// Caches cost plans by operation id.
/// </summary>
internal sealed class CostPlanCache(int capacity = 256)
{
    private readonly Cache<CostPlan> _cache = new(capacity);

    /// <summary>
    /// Gets the number of cost plans currently cached.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets the maximum number of cost plans that can be cached.
    /// </summary>
    public int Capacity => _cache.Capacity;

    /// <summary>
    /// Tries to get a cost plan by its <paramref name="operationId"/>.
    /// </summary>
    public bool TryGetPlan(string operationId, [NotNullWhen(true)] out CostPlan? plan)
        => _cache.TryGet(operationId, out plan);

    /// <summary>
    /// Tries to add a cost plan to the cache. The first plan added for an operation id wins.
    /// </summary>
    public void TryAddPlan(string operationId, CostPlan plan)
        => _cache.GetOrCreate(operationId, static (_, value) => value, plan);
}
