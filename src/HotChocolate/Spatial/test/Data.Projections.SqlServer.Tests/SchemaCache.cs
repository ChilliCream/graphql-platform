using System.Collections.Concurrent;
using HotChocolate.Execution;
using Squadron;

namespace HotChocolate.Data.Projections.Spatial;

public class SchemaCache : ProjectionVisitorTestBase, IDisposable
{
    private readonly ConcurrentDictionary<(Type, object), Task<IRequestExecutor>> _cache = new();

    public SchemaCache(PostgreSqlResource<PostgisConfig> resource) : base(resource)
    {
    }

    public Task<IRequestExecutor> CreateSchemaAsync<T>(T[] entities)
        where T : class
    {
        (Type, T[] entites) key = (typeof(T), entities);
        return _cache.GetOrAdd(key, _ => base.CreateSchemaAsync(entities));
    }

    public void Dispose()
    {
    }
}
