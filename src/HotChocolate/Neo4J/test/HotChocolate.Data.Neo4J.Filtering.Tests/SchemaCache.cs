using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Squadron;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class SchemaCache
        : FilteringTestBase, IDisposable
    {
        private Neo4jResource _resource = null!;

        protected void Init(Neo4jResource resource)
        {
            _resource = resource;
        }

        private readonly ConcurrentDictionary<(Type, object), Task<IRequestExecutor>> _cache = new();

        protected Task<IRequestExecutor> CreateSchema<T, TType>(string query, bool withPaging = false)
            where T : class
            where TType : FilterInputType<T>
        {
            (Type, Type) key = (typeof(T), typeof(TType));
            return _cache.GetOrAdd(
                key,
                k => base.CreateSchema<T, TType>(_resource, query, withPaging: withPaging));
        }

        public void Dispose()
        {
        }
    }
}
