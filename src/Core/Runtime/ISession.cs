using System;

namespace HotChocolate.Runtime
{
    public interface ISession
        : IDisposable
    {
        IDataLoaderProvider DataLoaders { get; }
        ICustomContextProvider CustomContexts { get; }
    }
}
