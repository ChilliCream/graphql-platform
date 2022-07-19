using System.Collections.Concurrent;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Squadron;

namespace HotChocolate.Data.Spatial.Filters;

public class SchemaCache
    : FilterVisitorTestBase
    , IDisposable
{
    private readonly ConcurrentDictionary<(Type, Type, object), Task<IRequestExecutor>> _cache =
        new();

    public SchemaCache(PostgreSqlResource<PostgisConfig> resource) : base(resource)
    {
    }

    public Task<IRequestExecutor> CreateSchemaAsync<T, TType>(T[] entities)
        where T : class
        where TType : FilterInputType<T>
    {
        (Type, Type, T[] entites) key = (typeof(T), typeof(TType), entities);
        return _cache.GetOrAdd(key, _ => base.CreateSchemaAsync<T, TType>(entities));
    }

    public void Dispose()
    {
    }
}
