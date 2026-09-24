using System.Diagnostics.CodeAnalysis;
using HotChocolate.Caching.Memory;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Caching;

/// <summary>
/// Caches operation plans by operation id.
/// </summary>
internal sealed class OperationPlanCache(int capacity, CacheDiagnostics? diagnostics)
{
    private readonly Cache<OperationPlan> _cache = new(capacity, diagnostics);

    /// <summary>
    /// Gets the number of operation plans currently cached.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets the maximum number of operation plans that can be cached.
    /// </summary>
    public int Capacity => _cache.Capacity;

    /// <summary>
    /// Tries to get an operation plan by its <paramref name="operationId"/>.
    /// </summary>
    public bool TryGetPlan(string operationId, [NotNullWhen(true)] out OperationPlan? plan)
        => _cache.TryGet(operationId, out plan);

    /// <summary>
    /// Tries to add an operation plan to the cache. The first plan added for an operation id wins.
    /// </summary>
    public void TryAddPlan(string operationId, OperationPlan plan)
        => _cache.TryAdd(operationId, plan);
}
