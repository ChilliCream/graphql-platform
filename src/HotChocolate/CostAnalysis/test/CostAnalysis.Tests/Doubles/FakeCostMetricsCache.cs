using System.Diagnostics.CodeAnalysis;
using HotChocolate.Caching.Memory;
using HotChocolate.CostAnalysis.Caching;

namespace HotChocolate.CostAnalysis.Doubles;

internal class FakeCostMetricsCache(int capacity = 256) : ICostMetricsCache
{
    private readonly Cache<CostMetrics> _cache = new(capacity);

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Count;

    public int Hits { get; private set; }

    public int Misses { get; private set; }

    public int Additions { get; private set; }

    public bool TryGetCostMetrics(string operationId, [NotNullWhen(true)] out CostMetrics? costMetrics)
    {
        var result = _cache.TryGet(operationId, out costMetrics);

        if (result)
        {
            Hits++;
        }
        else
        {
            Misses++;
        }

        return result;
    }

    public void TryAddCostMetrics(string operationId, CostMetrics costMetrics)
    {
        _cache.GetOrCreate(operationId, static (_, m) => m, costMetrics);
        Additions++;
    }
}
