using System;
using System.Collections.Generic;
using System.Linq;

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public class RequestServiceProvider
        : IServiceProvider
    {
        private readonly IServiceProvider _rootServiceProvider;
        private readonly Dictionary<Type, object> _services;

        public RequestServiceProvider(
            IServiceProvider rootServiceProvider,
            IEnumerable<KeyValuePair<Type, object>> services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _rootServiceProvider = rootServiceProvider ??
                throw new ArgumentNullException(nameof(rootServiceProvider));
            _services = services.ToDictionary(t => t.Key, t => t.Value);
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (!_services.TryGetValue(serviceType, out object instance))
            {
                instance = _rootServiceProvider.GetService(serviceType);
            }

            return instance;
        }
    }
}
