using System;

namespace Zeus
{
    internal class DefaultServiceProvider
        : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            return serviceType.Assembly.CreateInstance(serviceType.FullName);
        }
    }
}