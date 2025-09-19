using System.Collections.Concurrent;
using HotChocolate.Execution;

namespace HotChocolate.Data.Sorting;

public class SchemaCache : SortVisitorTestBase, IDisposable
{
    private readonly ConcurrentDictionary<(Type, Type, object), IRequestExecutor> _cache = new();

    public IRequestExecutor CreateSchema<T, TType>(
        T?[] entities,
        Action<ISchemaBuilder>? configure = null,
        SortConvention? convention = null)
        where T : class
        where TType : SortInputType<T>
    {
        (Type, Type, T?[] entites) key = (typeof(T), typeof(TType), entities);
        return _cache.GetOrAdd(
            key,
            _ => CreateSchema<T, TType>(entities, convention, configure));
    }

    public void Dispose() { }
}
