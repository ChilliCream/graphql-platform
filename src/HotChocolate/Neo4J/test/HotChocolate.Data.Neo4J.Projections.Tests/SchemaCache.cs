using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Squadron;

namespace HotChocolate.Data.Neo4J.Projections
{
    public class SchemaCache
        : ProjectionsTestBase
            , IDisposable
    {
        private readonly ConcurrentDictionary<(Type, object), Task<IRequestExecutor>> _cache = new();

        private readonly Neo4jResource _resource;

        public SchemaCache(Neo4jResource resource)
        {
            _resource = resource;
        }

        public Task<IRequestExecutor> CreateSchema<T>(
            string query,
            bool usePaging = false,
            ObjectType<T>? objectType = null)
            where T : class
        {
            (Type, string) key = (typeof(T), query);

            return _cache.GetOrAdd(
                key,
                k => ProjectionsTestBase.CreateSchema<T>(
                    _resource,
                    query));
        }

        public void Dispose()
        {
        }
    }
}
