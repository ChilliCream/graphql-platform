using System;
using GreenDonut;

#nullable enable

namespace HotChocolate.DataLoader
{
    /// <summary>
    /// The DataLoader-registry holds the instances of DataLoaders
    /// that are used by the execution engine.
    /// </summary>
    public interface IDataLoaderRegistry : IDisposable
    {
        T GetOrRegister<T>(
            string key,
            Func<T> createDataLoader)
            where T : IDataLoader;

        T GetOrRegister<T>(
            Func<T> createDataLoader)
            where T : IDataLoader;
    }
}
