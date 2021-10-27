using System;
using System.Collections.Concurrent;
using System.Linq;
using GreenDonut;
using static HotChocolate.Fetching.Properties.FetchingResources;

#nullable enable

namespace HotChocolate.Fetching
{
    public class DefaultDataLoaderRegistry : IDataLoaderRegistry
    {
        private readonly ConcurrentDictionary<string, IDataLoader> _dataLoaders = new();
        private bool _disposed;

        public T GetOrRegister<T>(string key, Func<T> createDataLoader) where T : IDataLoader
        {
            if (_dataLoaders.GetOrAdd(key, s => createDataLoader()) is T dataLoader)
            {
                return dataLoader;
            }

            throw new RegisterDataLoaderException(
                string.Format(
                    DefaultDataLoaderRegistry_GetOrRegister,
                    key,
                    typeof(T).FullName));
        }

        public T GetOrRegister<T>(Func<T> createDataLoader) where T : IDataLoader =>
            GetOrRegister(typeof(T).FullName ?? typeof(T).Name, createDataLoader);

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (IDisposable disposable in _dataLoaders.Values.OfType<IDisposable>())
                {
                    disposable.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
