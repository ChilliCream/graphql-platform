using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Types;

namespace HotChocolate.Internal
{
    internal sealed class ServiceManager
        : IServiceProvider
        , IDisposable
    {
        private readonly object _sync = new object();
        private readonly Stack<IServiceProvider> _serviceProviders =
            new Stack<IServiceProvider>();
        private readonly ServiceFactory _factory;
        private readonly ServiceContainer _types;
        private bool _disposed;


        public ServiceManager()
        {
            _factory = new ServiceFactory(t => GetServiceFromProviders(t));
            _types = new ServiceContainer(_factory);
        }

        public void RegisterServiceProvider(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (!_serviceProviders.Contains(services))
            {
                _serviceProviders.Push(services);
            }
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (IsNamedType(serviceType))
            {
                return _types.GetService(serviceType);
            }
            return GetServiceFromProviders(serviceType);
        }

        private object GetServiceFromProviders(Type serviceType)
        {
            foreach (IServiceProvider services in _serviceProviders)
            {
                object service = services.GetService(serviceType);
                if (service != null)
                {
                    return service;
                }
            }
            return null;
        }

        private bool IsNamedType(Type serviceType) =>
            typeof(INamedType).IsAssignableFrom(serviceType);


        public void Dispose()
        {
            _types.Dispose();
        }
    }
}
