using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Caching;

internal sealed class DefaultPreparedOperationCache(int capacity = 256) : IPreparedOperationCache
{
    private readonly Cache<IOperation> _cache = new(capacity);

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Count;

    public void TryAddOperation(string operationId, IOperation operation)
        => _cache.GetOrCreate(operationId, static (_, op) => op, operation);

    public bool TryGetOperation(string operationId, [NotNullWhen(true)] out IOperation? operation)
        => _cache.TryGet(operationId, out operation);

    public void Clear() => _cache.Clear();
}
