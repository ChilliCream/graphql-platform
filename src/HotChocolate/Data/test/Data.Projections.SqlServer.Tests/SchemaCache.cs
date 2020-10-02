using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using HotChocolate.Execution;

namespace HotChocolate.Data.Projections
{
    public class SchemaCache
        : ProjectionVisitorTestBase,
          IDisposable
    {
        private readonly ConcurrentDictionary<(Type, object), IRequestExecutor> _cache =
            new ConcurrentDictionary<(Type, object), IRequestExecutor>();

        public IRequestExecutor CreateSchema<T>(
            T[] entities)
            where T : class
        {
            (Type, T[] entites) key = (typeof(T), entities);
            return _cache.GetOrAdd(key, k => base.CreateSchema(entities));
        }

        public void Dispose()
        {
        }
    }
}
