using System;
using System.Collections.Concurrent;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using Squadron;

namespace HotChocolate.Data.MongoDb.Sorting
{
    public class SchemaCache
        : SortVisitorTestBase
        , IDisposable
    {
        private MongoResource _resource = null!;

        protected void Init(MongoResource resource)
        {
            _resource = resource;
        }

        private readonly ConcurrentDictionary<(Type, Type, object), IRequestExecutor> _cache =
            new ConcurrentDictionary<(Type, Type, object), IRequestExecutor>();

        public IRequestExecutor CreateSchema<T, TType>(T[] entities)
            where T : class
            where TType : SortInputType<T>
        {
            (Type, Type, T?[] entites) key = (typeof(T), typeof(TType), entities);
            return _cache.GetOrAdd(
                key,
                k => base.CreateSchema<T, TType>(entities, _resource));
        }

        public void Dispose()
        {
        }
    }
}
