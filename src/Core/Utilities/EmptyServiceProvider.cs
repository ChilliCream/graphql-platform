using System;

namespace HotChocolate.Utilities
{
    public sealed class EmptyServiceProvider
        : IServiceProvider
    {
        public object GetService(Type serviceType) => null;
    }
}
