using System;

namespace HotChocolate.Internal
{
    internal sealed class DefaultServiceProvider
        : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            return Activator.CreateInstance(serviceType);
        }
    }
}
