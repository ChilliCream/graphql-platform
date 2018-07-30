using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Runtime
{
    public class StateObjectContainer<TKey>
        : IDisposable
    {
        private readonly IServiceProvider _globalServices;
        private readonly IServiceProvider _requestServices;
        private readonly StateObjectDescriptorCollection<TKey> _descriptors;
        private readonly StateObjectCollection<TKey> _globalStates;
        private readonly StateObjectCollection<TKey> _requestStates;
        private bool _disposed;

        protected StateObjectContainer(
            IServiceProvider globalServices,
            IServiceProvider requestServices,
            StateObjectDescriptorCollection<TKey> descriptors,
            StateObjectCollection<TKey> globalStates)
        {
            _globalServices = globalServices
                ?? throw new ArgumentNullException(nameof(globalServices));
            _descriptors = descriptors
                ?? throw new ArgumentNullException(nameof(descriptors));
            _globalStates = globalStates
                ?? throw new ArgumentNullException(nameof(globalStates));
            _requestServices = requestServices ?? globalServices;
        }

        protected object GetStateObject(TKey key)
        {
            if (_descriptors.TryGetDescriptor(key,
                out IScopedStateDescriptor<TKey> descriptor))
            {
                IServiceProvider services = _globalServices;
                StateObjectCollection<TKey> stateObjects = _globalStates;

                if (descriptor.Scope == ExecutionScope.Request)
                {
                    services = _requestServices;
                    stateObjects = _requestStates;
                }

                if (stateObjects.TryGetObject(key, out object instance))
                {
                    return instance;
                }

                return stateObjects.CreateObject(descriptor,
                    _descriptors.CreateFactory(services, descriptor));
            }

            return null;
        }

        protected bool TryGetStateObjectDescriptor<T>(TKey key, out T descriptor)
            where T : IScopedStateDescriptor<TKey>
        {
            if (_descriptors.TryGetDescriptor(key,
                out IScopedStateDescriptor<TKey> d)
                && d is T t)
            {
                descriptor = t;
                return true;
            }

            descriptor = default(T);
            return false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _requestStates.Dispose();
                _disposed = true;
            }
        }
    }
}
