using System;
using HotChocolate.Runtime;

namespace HotChocolate
{
    public interface ICustomContextConfiguration
        : IFluent
    {
        void RegisterCustomContext<T>(
            ExecutionScope scope,
            Func<IServiceProvider, T> contextFactory = null);
    }
}
