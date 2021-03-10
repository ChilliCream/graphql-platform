using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using Squadron;

namespace HotChocolate.Data.Neo4J.Sorting
{
    public class SchemaCache
        : SortingTestBase
            , IDisposable
    {
        private Neo4jResource _resource = null!;

        protected void Init(Neo4jResource resource)
        {
            _resource = resource;
        }

        private readonly ConcurrentDictionary<(Type, Type, object), Task<IRequestExecutor>> _cache = new();

        public Task<IRequestExecutor> CreateSchema<T, TType>(string query)
            where T : class
            where TType : SortInputType<T>
        {
            (Type, Type, string) key = (typeof(T), typeof(TType), query);

            return _cache.GetOrAdd(
                key,
                k => base.CreateSchema<T, TType>(_resource, query));
        }

        public void Dispose()
        {
        }
    }
}
