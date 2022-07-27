using System;
using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;

namespace HotChocolate.Data.Filters;

public class SchemaCache
    : FilterVisitorTestBase
    , IDisposable
{
    private readonly ConcurrentDictionary<(Type, Type, object), IRequestExecutor> _cache = new();

    public IRequestExecutor CreateSchema<T, TType>(
        T[] entities,
        bool withPaging = false,
        Action<IRequestExecutorBuilder>? configure = null)
        where T : class
        where TType : FilterInputType<T>
    {
        (Type, Type, T[] entites) key = (typeof(T), typeof(TType), entities);
        return _cache.GetOrAdd(
            key,
            k => base.CreateSchema<T, TType>(
                entities,
                withPaging: withPaging,
                configure: configure));
    }

    public void Dispose()
    {
    }
}
