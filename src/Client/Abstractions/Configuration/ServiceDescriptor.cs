
using System;

namespace StrawberryShake.Configuration
{
    public sealed class ServiceDescriptor
    {
        public ServiceDescriptor(
            Type serviceType,
            Func<IServiceProvider, object> factory,
            ServiceLifetime lifetime = default)
        {
            ServiceType = serviceType;
            Factory = factory;
            Lifetime = lifetime;
        }

        public Type ServiceType { get; }

        public Func<IServiceProvider, object> Factory { get; }

        public ServiceLifetime Lifetime { get; }
    }
}
