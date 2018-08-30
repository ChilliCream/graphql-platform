using System;
using System.Collections.Concurrent;

namespace HotChocolate.Resolvers
{
    internal sealed class ResolverCache
        : IResolverCache
    {
        private readonly ConcurrentDictionary<Type, object> _internalCache =
            new ConcurrentDictionary<Type, object>();

        public T AddOrGetResolver<T>(Func<T> resolverFactory)
        {
            T resolver = resolverFactory();
            if (!_internalCache.TryAdd(typeof(T), resolver))
            {
                if (resolver is IDisposable d)
                {
                    d.Dispose();
                }

                if (_internalCache.TryGetValue(typeof(T), out object r))
                {
                    return (T)r;
                }

                throw new InvalidOperationException(
                    "The resolver could not fetched or added.");
            }
            return resolver;
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
