using System;

namespace StrawberryShake.Http.Utilities
{
    internal sealed class EmptyServiceProvider
        : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
