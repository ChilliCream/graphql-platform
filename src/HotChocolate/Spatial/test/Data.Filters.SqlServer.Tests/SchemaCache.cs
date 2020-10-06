using System;
using System.Collections.Concurrent;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Squadron;

namespace HotChocolate.Spatial.Data.Filters
{
    public class SchemaCache
        : FilterVisitorTestBase,
          IDisposable
    {
        private readonly ConcurrentDictionary<(Type, Type, object), IRequestExecutor> _cache =
            new ConcurrentDictionary<(Type, Type, object), IRequestExecutor>();

        public SchemaCache(PostgreSqlResource<PostgisConfig> resouce) : base(resouce)
        {
        }

        public IRequestExecutor CreateSchema<T, TType>(T[] entities)
            where T : class
            where TType : FilterInputType<T>
        {
            (Type, Type, T[] entites) key = (typeof(T), typeof(TType), entities);
            return _cache.GetOrAdd(key, (k) => base.CreateSchema<T, TType>(entities));
        }

        public void Dispose()
        {
        }
    }
}
