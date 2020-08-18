using System;
using System.Collections.Concurrent;
using System.Linq;
using GreenDonut;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.DataLoader
{
    public class DefaultDataLoaderRegistry : IDataLoaderRegistry
    {
        private readonly ConcurrentDictionary<string, IDataLoader> _dataLoaders =
            new ConcurrentDictionary<string, IDataLoader>();

        public T GetOrRegister<T>(
            string key,
            Func<T> createDataLoader)
            where T : IDataLoader
        {
            if (_dataLoaders.GetOrAdd(key, s => createDataLoader()) is T dataLoader)
            {
                return dataLoader;
            }

            throw new RegisterDataLoaderException(
                string.Format(
                    TypeResources.DefaultDataLoaderRegistry_GetOrRegister,
                    key,
                    typeof(T).FullName));
        }

        public T GetOrRegister<T>(
            Func<T> createDataLoader)
            where T : IDataLoader =>
            GetOrRegister<T>(typeof(T).FullName ?? typeof(T).Name, createDataLoader);

        public void Dispose()
        {
            foreach (IDisposable disposable in _dataLoaders.Values.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
        }
    }
}
