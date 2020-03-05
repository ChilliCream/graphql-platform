using System;
using GreenDonut;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.DataLoader
{
    public static class DataLoaderRegistryExtensions
    {
        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchBatchFactory<TKey, TValue> factory)
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

            return registry.Register(key, services =>
                new FetchBatchDataLoader<TKey, TValue>(
                    factory(services)));
        }

        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchBatch<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    TypeResources.DataLoaderRegistry_KeyNullOrEmpty,
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return registry.Register(key, services =>
                new FetchBatchDataLoader<TKey, TValue>(fetch));
        }

        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchGroupeFactory<TKey, TValue> factory)
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

            return registry.Register(key, services =>
                new FetchGroupedDataLoader<TKey, TValue>(
                    factory(services)));
        }

        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchGroup<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    TypeResources.DataLoaderRegistry_KeyNullOrEmpty,
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return registry.Register(key, services =>
                new FetchGroupedDataLoader<TKey, TValue>(fetch));
        }

        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchCacheFactory<TKey, TValue> factory)
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

            return registry.Register(key, services =>
                new FetchSingleDataLoader<TKey, TValue>(
                    factory(services)));
        }

        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchCache<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    TypeResources.DataLoaderRegistry_KeyNullOrEmpty,
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return registry.Register(key, services =>
                new FetchSingleDataLoader<TKey, TValue>(fetch));
        }

        public static bool Register<TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchOnceFactory<TValue> factory)
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

            return registry.Register(key, services =>
            {
                FetchOnce<TValue> fetch = factory(services);
                return new FetchSingleDataLoader<string, TValue>(
                    k => fetch(), DataLoaderDefaults.MinCacheSize);
            });
        }

        public static bool Register<TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchOnce<TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    TypeResources.DataLoaderRegistry_KeyNullOrEmpty,
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return registry.Register(key, services =>
            {
                return new FetchSingleDataLoader<string, TValue>(
                    k => fetch(), DataLoaderDefaults.MinCacheSize);
            });
        }

        public static bool Register<T>(
            this IDataLoaderRegistry registry,
            string key)
            where T : class, IDataLoader
        {
            Func<IServiceProvider, T> createInstance =
                ActivatorHelper.CompileFactory<T>();
            return registry.Register(key, createInstance);
        }

        public static bool Register<T>(
            this IDataLoaderRegistry registry)
            where T : class, IDataLoader
        {
            Func<IServiceProvider, T> createInstance =
                ActivatorHelper.CompileFactory<T>();
            return registry.Register(typeof(T).FullName, createInstance);
        }
    }
}
