using System;
using System.Collections.Immutable;

namespace HotChocolate.Internal
{
    internal class ServiceContainer
        : IServiceProvider
        , IDisposable
    {
        private readonly object _sync = new object();
        private readonly ServiceFactory _serviceFactory;
        private ImmutableDictionary<Type, object> _services =
            ImmutableDictionary<Type, object>.Empty;

        public ServiceContainer(ServiceFactory serviceFactory)
        {
            if (serviceFactory == null)
            {
                throw new ArgumentNullException(nameof(serviceFactory));
            }

            _serviceFactory = serviceFactory;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (!_services.TryGetValue(serviceType, out object service))
            {

                service = _serviceFactory.TryCreateInstance(serviceType);
                if (service != null)
                {
                    lock (_sync)
                    {
                        _services = _services.SetItem(serviceType, service);
                    }
                }
            }

            return service;
        }

        private void FinalizeService(object service)
        {
            if (service is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch { }
            }
        }

        public void Dispose()
        {
            ImmutableDictionary<Type, object> services = _services;
            _services = ImmutableDictionary<Type, object>.Empty;

            foreach (object service in services.Values)
            {
                FinalizeService(service);
            }
        }
    }
}
