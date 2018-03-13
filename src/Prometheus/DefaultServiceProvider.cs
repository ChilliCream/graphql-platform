using System;

namespace Prometheus
{
    internal class DefaultServiceProvider
        : IServiceProvider
    {
        private DefaultServiceProvider() { }

        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            return serviceType.Assembly.CreateInstance(serviceType.FullName);
        }

        public static DefaultServiceProvider Instance { get; } = new DefaultServiceProvider();
    }
}