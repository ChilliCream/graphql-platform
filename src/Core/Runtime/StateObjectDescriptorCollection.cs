using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Runtime
{
    public class StateObjectDescriptorCollection<TKey>
    {
        private readonly Dictionary<TKey, IScopedStateDescriptor<TKey>> _descriptors;

        public StateObjectDescriptorCollection(
            IEnumerable<IScopedStateDescriptor<TKey>> descriptors)
        {
            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            _descriptors = descriptors.ToDictionary(t => t.Key);
        }

        public bool TryGetDescriptor(
            TKey key,
            out IScopedStateDescriptor<TKey> descriptor)
        {
            return _descriptors.TryGetValue(key, out descriptor);
        }

        public Func<object> CreateFactory(
            IServiceProvider services,
            IScopedStateDescriptor<TKey> descriptor)
        {
            if (descriptor.Factory != null)
            {
                return () => descriptor.Factory(services);
            }

            var serviceFactory = new ServiceFactory();
            serviceFactory.Services = services;
            return () => serviceFactory.CreateInstance(descriptor.Type);
        }
    }
}
