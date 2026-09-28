using System.Diagnostics.CodeAnalysis;
using HotChocolate.Caching.Memory;

namespace HotChocolate.CostAnalysis;

internal sealed class CostPlanCache(int capacity)
{
    private readonly Cache<CostPlan> _cache = new(capacity);

    /// <summary>
    /// Gets the number of cost plans currently held by the cache.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets the maximum number of cost plans the cache can hold.
    /// </summary>
    public int Capacity => _cache.Capacity;

    public bool TryGetPlan(string operationId, [NotNullWhen(true)] out CostPlan? plan)
        => _cache.TryGet(operationId, out plan);

    public void TryAddPlan(string operationId, CostPlan plan)
        => _cache.GetOrCreate(operationId, static (_, value) => value, plan);
}
