using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Types;
using Squadron;

namespace HotChocolate.Data.MongoDb.Projections;

public class SchemaCache
    : ProjectionVisitorTestBase
    , IDisposable
{
    private readonly ConcurrentDictionary<(Type, object), IRequestExecutor> _cache = new();

    private readonly MongoResource _resource;

    public SchemaCache(MongoResource resource)
    {
        _resource = resource;
    }

    public IRequestExecutor CreateSchema<T>(
        T[] entities,
        bool usePaging = false,
        bool useOffsetPaging = false,
        ObjectType<T>? objectType = null)
        where T : class
    {
        (Type, T[] entites) key = (typeof(T), entities);
        return _cache.GetOrAdd(
            key,
            _ => base.CreateSchema(
                entities,
                usePaging: usePaging,
                useOffsetPaging: useOffsetPaging,
                mongoResource: _resource,
                objectType: objectType));
    }

    public void Dispose()
    {
    }
}
