using System.Collections.Concurrent;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;

namespace HotChocolate.Data;

public class SchemaCache : SortVisitorTestBase
{
    private readonly ConcurrentDictionary<(Type, Type, object), Task<IRequestExecutor>> _cache = new();

    public async Task<IRequestExecutor> CreateSchemaAsync<T, TType>(T[] entities)
        where T : class
        where TType : SortInputType<T>
    {
        var key = (typeof(T), typeof(TType), entities);
        return await _cache.GetOrAdd(key, async (k) => await base.CreateSchemaAsync<T, TType>(entities));
    }
}
