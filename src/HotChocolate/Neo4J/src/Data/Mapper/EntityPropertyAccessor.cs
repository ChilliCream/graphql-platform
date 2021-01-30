using System;
using System.Collections.Concurrent;

namespace HotChocolate.Data.Neo4J
{
    internal static class EntityPropertyAccessor
    {
        private static readonly ConcurrentDictionary<Type, IEntityPropertyAccessor> CachedPropertyAccessors = new ();

        public static long? GetNodeId<T>(T entity)
        {
            return CachedPropertyAccessors
                .GetOrAdd(typeof(T), new EntityPropertyAccessor<T>())
                .GetNodeId(entity);
        }

        public static void SetNodeId<T>(T entity, long id)
        {
            CachedPropertyAccessors
                .GetOrAdd(typeof(T), new EntityPropertyAccessor<T>())
                .SetNodeId(entity, id);
        }
    }
}
