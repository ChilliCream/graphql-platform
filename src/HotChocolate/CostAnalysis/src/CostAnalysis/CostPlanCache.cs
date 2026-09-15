using System.Diagnostics.CodeAnalysis;
using HotChocolate.Caching.Memory;

namespace HotChocolate.CostAnalysis;

internal sealed class CostPlanCache(int capacity)
{
    private readonly Cache<CostPlan> _cache = new(capacity);

    public Cache<CostPlan> InnerCache => _cache;

    public bool TryGetPlan(string operationId, [NotNullWhen(true)] out CostPlan? plan)
        => _cache.TryGet(operationId, out plan);

    public void TryAddPlan(string operationId, CostPlan plan)
        => _cache.GetOrCreate(operationId, static (_, value) => value, plan);
}
