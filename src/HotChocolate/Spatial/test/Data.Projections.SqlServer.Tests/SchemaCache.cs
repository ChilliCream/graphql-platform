using System.Collections.Concurrent;
using HotChocolate.Execution;

namespace HotChocolate.Data.Projections.Spatial;

public class SchemaCache(PostgisResource resource)
    : ProjectionVisitorTestBase(resource)
    , IDisposable
{
    private readonly ConcurrentDictionary<(Type, object), Task<IRequestExecutor>> _cache = new();

    public Task<IRequestExecutor> CreateSchemaAsync<T>(T[] entities)
        where T : class
    {
        (Type, T[] entites) key = (typeof(T), entities);
        return _cache.GetOrAdd(key, _ => base.CreateSchemaAsync(entities));
    }

    public void Dispose() { }
}
