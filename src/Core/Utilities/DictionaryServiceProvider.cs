using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Utilities
{
    public sealed class DictionaryServiceProvider
        : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services;

        public DictionaryServiceProvider(
            params KeyValuePair<Type, object>[] services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services.ToDictionary(t => t.Key, t => t.Value);
        }

        public DictionaryServiceProvider(
            IEnumerable<KeyValuePair<Type, object>> services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services.ToDictionary(t => t.Key, t => t.Value);
        }

        public object GetService(Type serviceType)
        {
            if (_services.TryGetValue(serviceType, out object service))
            {
                return service;
            }
            return null;
        }
    }
}
