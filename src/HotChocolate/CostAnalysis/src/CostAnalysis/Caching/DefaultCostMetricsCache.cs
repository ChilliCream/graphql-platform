using System.Diagnostics.CodeAnalysis;
using HotChocolate.Caching.Memory;

namespace HotChocolate.CostAnalysis.Caching;

internal sealed class DefaultCostMetricsCache(int capacity = 256) : ICostMetricsCache
{
    private readonly Cache<CostMetrics> _cache = new(capacity);

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Count;

    public bool TryGetCostMetrics(
        string operationId,
        [NotNullWhen(true)] out CostMetrics? costMetrics)
        => _cache.TryGet(operationId, out costMetrics);

    public void TryAddCostMetrics(string operationId, CostMetrics costMetrics)
        => _cache.GetOrCreate(operationId, static (_, m) => m, costMetrics);
}
