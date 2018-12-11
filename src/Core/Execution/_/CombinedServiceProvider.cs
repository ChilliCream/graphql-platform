using System;

namespace HotChocolate.Execution
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

    internal static class ServiceProviderExtensions
    {
        public static IServiceProvider Include(
            this IServiceProvider first,
            IServiceProvider second)
        {
            return new CombinedServiceProvider(first, second);
        }
    }
}
