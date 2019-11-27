using System;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake.Http.Utilities
{
    internal sealed class DictionaryServiceProvider
        : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services;

        public DictionaryServiceProvider(Type service, object instance)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            _services = Initialize(new Dictionary<Type, List<object>>
            {
                { service, new List<object> { instance } }
            });
        }

        public DictionaryServiceProvider(
            params KeyValuePair<Type, List<object>>[] services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = Initialize(services.ToDictionary(t => t.Key, t => t.Value));
        }

        public DictionaryServiceProvider(
            IEnumerable<KeyValuePair<Type, List<object>>> services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = Initialize(services.ToDictionary(t => t.Key, t => t.Value));
        }

        private Dictionary<Type, object> Initialize(
            Dictionary<Type, List<object>> services)
        {
            var current = new Dictionary<Type, object>();

            foreach (KeyValuePair<Type, List<object>> item in services)
            {
                if (item.Key.IsGenericType
                    && item.Key.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    Type serviceType = item.Key.GetGenericArguments().First();
                    if (!current.ContainsKey(serviceType))
                    {
                        current.Add(serviceType, item.Value[0]);
                    }
                }
                else
                {
                    Type servicesType = typeof(IEnumerable<>).MakeGenericType(item.Key);
                    if (!current.ContainsKey(servicesType))
                    {
                        current.Add(servicesType, item.Value);
                    }
                }
            }

            return current;
        }

        public object? GetService(Type serviceType)
        {
            if (_services.TryGetValue(serviceType, out object? service))
            {
                return service;
            }
            return null;
        }
    }
}
