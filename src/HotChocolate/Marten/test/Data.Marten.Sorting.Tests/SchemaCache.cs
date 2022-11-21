using System;
using System.Collections.Concurrent;
using HotChocolate.Execution;

namespace HotChocolate.Data.Sorting;

public class SchemaCache : SortVisitorTestBase
{
    private readonly ConcurrentDictionary<(Type, Type, object), IRequestExecutor> _cache = new();

    public IRequestExecutor CreateSchema<T, TType>(T[] entities)
        where T : class
        where TType : SortInputType<T>
    {
        var key = (typeof(T), typeof(TType), entities);
        return _cache.GetOrAdd(key, (k) => base.CreateSchema<T, TType>(entities));
    }
}
