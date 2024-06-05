using System.Diagnostics.CodeAnalysis;
using HotChocolate.Utilities;

namespace HotChocolate.CostAnalysis.Caching;

internal sealed class DefaultCostMetricsCache(int capacity = 100) : ICostMetricsCache
{
    private readonly Cache<CostMetrics> _cache = new(capacity);

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Usage;

    public bool TryGetCostMetrics(
        string operationId,
        [NotNullWhen(true)] out CostMetrics? costMetrics)
        => _cache.TryGet(operationId, out costMetrics);

    public void TryAddCostMetrics(
        string operationId,
        CostMetrics costMetrics)
        => _cache.GetOrCreate(operationId, () => costMetrics);

    public void Clear() => _cache.Clear();
}
