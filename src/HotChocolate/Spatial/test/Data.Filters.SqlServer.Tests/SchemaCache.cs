using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Squadron;

namespace HotChocolate.Spatial.Data.Filters
{
    public class SchemaCache
        : FilterVisitorTestBase,
          IDisposable
    {
        private readonly ConcurrentDictionary<(Type, Type, object), Task<IRequestExecutor>> _cache =
            new ConcurrentDictionary<(Type, Type, object), Task<IRequestExecutor>>();

        public SchemaCache(PostgreSqlResource<PostgisConfig> resouce) : base(resouce)
        {
        }

        public Task<IRequestExecutor> CreateSchemaAsync<T, TType>(T[] entities)
            where T : class
            where TType : FilterInputType<T>
        {
            (Type, Type, T[] entites) key = (typeof(T), typeof(TType), entities);
            return _cache.GetOrAdd(key, k => base.CreateSchemaAsync<T, TType>(entities));
        }

        public void Dispose()
        {
        }
    }
}
