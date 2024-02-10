using System;
using System.Collections.Concurrent;
using GreenDonut;
using static HotChocolate.Fetching.Properties.FetchingResources;

#nullable enable

namespace HotChocolate.Fetching;

public sealed class DefaultDataLoaderRegistry : IDataLoaderRegistry
{
    private readonly ConcurrentDictionary<string, IDataLoader> _dataLoaders = new();
    private bool _disposed;

    public T GetOrRegister<T>(string key, Func<T> createDataLoader) where T : IDataLoader
    {
        if (_dataLoaders.GetOrAdd(key, _ => createDataLoader()) is T dataLoader)
        {
            return dataLoader;
        }

        throw new RegisterDataLoaderException(
            string.Format(
                DefaultDataLoaderRegistry_GetOrRegister,
                key,
                typeof(T).FullName));
    }

    public T GetOrRegister<T>(Func<T> createDataLoader) where T : IDataLoader
        => GetOrRegister(typeof(T).FullName ?? typeof(T).Name, createDataLoader);

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var value in _dataLoaders.Values)
            {
                if (value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _disposed = true;
        }
    }
}
