using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetClassic.Helpers
{
    public class DefaultDependencyScope
        : IDependencyScope
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultDependencyScope(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ??
                throw new ArgumentNullException(nameof(serviceProvider));
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _serviceProvider.GetServices(serviceType);
        }

        public void Dispose() { }
    }
}
