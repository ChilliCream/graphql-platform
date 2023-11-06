using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Caching;

internal sealed class DefaultPreparedOperationCache : IPreparedOperationCache
{
    private readonly Cache<IOperation> _cache;

    public DefaultPreparedOperationCache(int capacity = 100)
    {
        _cache = new Cache<IOperation>(capacity);
    }

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Usage;

    public void TryAddOperation(
        string operationId,
        IOperation operation) =>
        _cache.GetOrCreate(operationId, () => operation);

    public bool TryGetOperation(
        string operationId,
        [NotNullWhen(true)] out IOperation? operation) =>
        _cache.TryGet(operationId, out operation!);

    public void Clear() => _cache.Clear();
}

