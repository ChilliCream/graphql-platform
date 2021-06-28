using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Squadron;

namespace HotChocolate.Data.Projections.Spatial
{
    public class SchemaCache
        : ProjectionVisitorTestBase
        , IDisposable
    {
        private readonly ConcurrentDictionary<(Type, object), Task<IRequestExecutor>> _cache =
            new ConcurrentDictionary<(Type, object), Task<IRequestExecutor>>();

        public SchemaCache(PostgreSqlResource<PostgisConfig> resouce) : base(resouce)
        {
        }

        public Task<IRequestExecutor> CreateSchemaAsync<T>(T[] entities)
            where T : class
        {
            (Type, T[] entites) key = (typeof(T), entities);
            return _cache.GetOrAdd(key, k => base.CreateSchemaAsync<T>(entities));
        }

        public void Dispose()
        {
        }
    }
}
