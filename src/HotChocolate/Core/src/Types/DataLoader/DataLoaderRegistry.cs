using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using GreenDonut;
using HotChocolate.Properties;

namespace HotChocolate.DataLoader
{
    public sealed class DataLoaderRegistry
        : IDataLoaderRegistry
        , IDisposable
    {
        private readonly object _sync = new object();
        private readonly ConcurrentDictionary<string, Func<IDataLoader>> _factories =
            new ConcurrentDictionary<string, Func<IDataLoader>>();
        private readonly ConcurrentDictionary<string, IDataLoader> _instances =
            new ConcurrentDictionary<string, IDataLoader>();
        private ImmutableHashSet<IObserver<IDataLoader>> _observers =
            ImmutableHashSet<IObserver<IDataLoader>>.Empty;
        private readonly IServiceProvider _services;

        public DataLoaderRegistry(IServiceProvider services)
        {
            _services = services
                ?? throw new ArgumentNullException(nameof(services));
        }

        public IDisposable Subscribe(IObserver<IDataLoader> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            lock (_sync)
            {
                _observers = _observers.Add(observer);
            }

            return new ObserverSession(() =>
            {
                lock (_sync)
                {
                    _observers = _observers.Remove(observer);
                }
            });
        }

        public bool Register<T>(string key, Func<IServiceProvider, T> factory)
            where T : IDataLoader
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    TypeResources.DataLoaderRegistry_KeyNullOrEmpty,
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return _factories.TryAdd(key, () => factory(_services));
        }

        public bool TryGet<T>(string key, out T dataLoader)
            where T : IDataLoader
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    TypeResources.DataLoaderRegistry_KeyNullOrEmpty,
                    nameof(key));
            }

            if (!_instances.TryGetValue(key, out IDataLoader loader)
                && _factories.TryGetValue(key, out Func<IDataLoader> factory))
            {
                lock (factory)
                {
                    if (!_instances.TryGetValue(key, out loader))
                    {
                        loader = _instances.GetOrAdd(key, k => factory());
                        NotifyObservers(loader);
                    }
                }
            }

            if (loader is T l)
            {
                dataLoader = l;
                return true;
            }

            dataLoader = default(T);
            return false;
        }

        private void NotifyObservers(IDataLoader dataLoader)
        {
            foreach (IObserver<IDataLoader> observer in _observers)
            {
                observer.OnNext(dataLoader);
            }
        }

        public void Dispose()
        {
            foreach (IObserver<IDataLoader> observer in _observers)
            {
                observer.OnCompleted();
            }

            foreach (IDataLoader dataLoader in _instances.Values)
            {
                if (dataLoader is IDisposable d)
                {
                    d.Dispose();
                }
            }

            _observers = _observers.Clear();
            _instances.Clear();
        }

        private class ObserverSession
            : IDisposable
        {
            private readonly Action _unregister;

            public ObserverSession(Action unregister)
            {
                _unregister = unregister;
            }

            public void Dispose()
            {
                _unregister();
            }
        }
    }
}
