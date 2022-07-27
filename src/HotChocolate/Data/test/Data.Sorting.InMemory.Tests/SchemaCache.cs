using System;
using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;

namespace HotChocolate.Data.Sorting;

public class SchemaCache : SortVisitorTestBase, IDisposable
{
    private readonly ConcurrentDictionary<(Type, Type, object), IRequestExecutor> _cache = new();

    public IRequestExecutor CreateSchema<T, TType>(
        T?[] entities,
        Action<IRequestExecutorBuilder>? configure = null)
        where T : class
        where TType : SortInputType<T>
    {
        (Type, Type, T?[] entites) key = (typeof(T), typeof(TType), entities);
        return _cache.GetOrAdd(
            key,
            _ => base.CreateSchema<T, TType>(entities, configure: configure));
    }

    public void Dispose() { }
}
