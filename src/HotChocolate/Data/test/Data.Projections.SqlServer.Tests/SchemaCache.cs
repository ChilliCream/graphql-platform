using System;
using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Projections
{
    public class SchemaCache
        : ProjectionVisitorTestBase,
          IDisposable
    {
        private readonly ConcurrentDictionary<(Type, object), IRequestExecutor> _cache =
            new ConcurrentDictionary<(Type, object), IRequestExecutor>();

        public IRequestExecutor CreateSchema<T>(
            T[] entities,
            Action<ModelBuilder>? onModelCreating = null,
            bool usePaging = false,
            ObjectType<T>? objectType = null)
            where T : class
        {
            (Type, T[] entites) key = (typeof(T), entities);
            return _cache.GetOrAdd(
                key,
                k => base.CreateSchema(
                    entities,
                    usePaging: usePaging,
                    onModelCreating: onModelCreating,
                    objectType: objectType));
        }

        public void Dispose()
        {
        }
    }
}
