using System;

namespace HotChocolate.Execution
{
    internal class DefaultServiceProvider
        : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            return Activator.CreateInstance(serviceType);
        }
    }
}
