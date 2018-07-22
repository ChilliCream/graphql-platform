using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.AspNetCore
{
    public class RequestServiceProvider
        : IServiceProvider
    {
        private readonly IServiceProvider _root;
        private readonly Dictionary<Type, object> _services;

        public RequestServiceProvider(
            IServiceProvider root,
            IEnumerable<KeyValuePair<Type, object>> services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services.ToDictionary(t => t.Key, t => t.Value);
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (!_services.TryGetValue(serviceType, out object instance))
            {
                instance = _root.GetService(serviceType);
            }
            return instance;
        }
    }
}
