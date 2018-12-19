using System;

namespace HotChocolate.Utilities
{
    internal sealed class CombinedServiceProvider
        : IServiceProvider
    {
        private readonly IServiceProvider _first;
        private readonly IServiceProvider _second;

        public CombinedServiceProvider(
            IServiceProvider first,
            IServiceProvider second)
        {
            _first = first ?? throw new ArgumentNullException(nameof(first));
            _second = second ?? throw new ArgumentNullException(nameof(second));
        }

        public object GetService(Type serviceType)
        {
            return _first.GetService(serviceType)
                ?? _second.GetService(serviceType);
        }
    }
}
