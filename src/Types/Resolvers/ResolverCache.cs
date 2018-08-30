using System;
using System.Collections.Concurrent;

namespace HotChocolate.Resolvers
{
    internal sealed class ResolverCache
        : IResolverCache
    {
        private readonly ConcurrentDictionary<Type, object> _internalCache =
            new ConcurrentDictionary<Type, object>();

        public bool TryAddResolver<T>(T resolver)
        {
            return _internalCache.TryAdd(typeof(T), resolver);
        }

        public bool TryGetResolver<T>(out T resolver)
        {
            if (_internalCache.TryGetValue(typeof(T), out object res))
            {
                resolver = (T)res;
                return true;
            }

            resolver = default;
            return false;
        }
    }
}
