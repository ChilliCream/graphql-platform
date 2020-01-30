using System;
using System.Collections.Concurrent;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class Activator
        : IActivator
    {
        private readonly ConcurrentDictionary<Type, object> _resolverCache =
            new ConcurrentDictionary<Type, object>();
        private readonly IServiceProvider _services;
        private readonly ServiceFactory _serviceFactory;

        public Activator(IServiceProvider services)
        {
            _services = services
                ?? throw new ArgumentNullException(nameof(services));
            _serviceFactory = new ServiceFactory { Services = _services };
        }

        public T CreateInstance<T>()
        {
            return (T)_serviceFactory.CreateInstance(typeof(T));
        }

        public TResolver GetOrCreateResolver<TResolver>()
        {
            Type resolverType = typeof(TResolver);

            if (!_resolverCache.TryGetValue(resolverType, out var obj))
            {
                if (!(_services.GetService(typeof(TResolver)) is TResolver r))
                {
                    r = CreateInstance<TResolver>();
                }

                _resolverCache.TryAdd(resolverType, r);
                return r;
            }

            return (TResolver)obj;
        }
    }
}
