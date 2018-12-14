using System;

namespace HotChocolate.Runtime
{
    public interface ICustomContextProvider
        : IDisposable
    {
        T GetCustomContext<T>();
    }
}
