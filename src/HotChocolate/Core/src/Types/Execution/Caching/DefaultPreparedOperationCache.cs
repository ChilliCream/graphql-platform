using System.Diagnostics.CodeAnalysis;
using HotChocolate.Caching.Memory;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Caching;

internal sealed class DefaultPreparedOperationCache(int capacity = 256) : IPreparedOperationCache
{
    private readonly Cache<Operation> _cache = new(capacity);

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Count;

    public void TryAddOperation(string operationId, Operation operation)
        => _cache.GetOrCreate(operationId, static (_, op) => op, operation);

    public bool TryGetOperation(string operationId, [NotNullWhen(true)] out Operation? operation)
        => _cache.TryGet(operationId, out operation);
}
