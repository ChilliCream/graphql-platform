using System;

namespace HotChocolate.Utilities
{
    public static class ServiceProviderExtensions
    {
        public static IServiceProvider Include(
            this IServiceProvider first,
            IServiceProvider second)
        {
            return new CombinedServiceProvider(first, second);
        }
    }
}
