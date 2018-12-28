using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.DataLoader;

namespace HotChocolate.Resolvers
{
    public static class DataLoaderResolverContextExtensions
    {
        public static IDataLoader<TKey, TValue> DataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchFactory<TKey, TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (TryGetDataLoader(context, key,
                out IDataLoader<TKey, TValue> dataLoader,
                out IDataLoaderRegistry registry))
            {
                return dataLoader;
            }

            return GetOrCreate<IDataLoader<TKey, TValue>>(
                key, registry, r => r.Register(key, factory));
        }

        public static IDataLoader<TKey, TValue> DataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            Fetch<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return DataLoader(context, key, services => fetch);
        }

        public static IDataLoader<TKey, TValue[]> DataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchGroupedFactory<TKey, TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (TryGetDataLoader(context, key,
                out IDataLoader<TKey, TValue[]> dataLoader,
                out IDataLoaderRegistry registry))
            {
                return dataLoader;
            }

            return GetOrCreate<IDataLoader<TKey, TValue[]>>(
                key, registry, r => r.Register(key, factory));
        }

        public static IDataLoader<TKey, TValue[]> DataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchGrouped<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return DataLoader(context, key, services => fetch);
        }

        public static IDataLoader<TKey, TValue> DataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchSingleFactory<TKey, TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (TryGetDataLoader(context, key,
                out IDataLoader<TKey, TValue> dataLoader,
                out IDataLoaderRegistry registry))
            {
                return dataLoader;
            }

            return GetOrCreate<IDataLoader<TKey, TValue>>(
                key, registry, r => r.Register(key, factory));
        }

        public static IDataLoader<TKey, TValue> DataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchSingle<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return DataLoader(context, key, services => fetch);
        }

        public static Func<Task<TValue>> DataLoader<TValue>(
            this IResolverContext context,
            string key,
            FetchOnceFactory<TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (!TryGetDataLoader(context, key,
                out IDataLoader<string, TValue> dataLoader,
                out IDataLoaderRegistry registry))
            {
                dataLoader = GetOrCreate<IDataLoader<string, TValue>>(
                    key, registry, r => r.Register(key, factory));
            }

            return () => dataLoader.LoadAsync("none");
        }

        public static Func<Task<TValue>> DataLoader<TValue>(
            this IResolverContext context,
            string key,
            FetchOnce<TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return DataLoader(context, key, services => fetch);
        }

        private static bool TryGetDataLoader<T>(
            IResolverContext context,
            string key,
            out T dataLoader,
            out IDataLoaderRegistry registry)
        {
            registry = null;

            foreach (IDataLoaderRegistry current in
                context.Service<IEnumerable<IDataLoaderRegistry>>())
            {
                registry = current;

                if (current.TryGet(key, out dataLoader))
                {
                    return true;
                }
            }

            dataLoader = default(T);
            return false;
        }

        private static T GetOrCreate<T>(
            string key,
            IDataLoaderRegistry registry,
            Action<IDataLoaderRegistry> register)
        {
            if (registry == null)
            {
                // TODO : resources
                throw new InvalidOperationException(
                    "No DataLoader registry was registerd with your " +
                    "dependency injection.");
            }

            register(registry);

            if (!registry.TryGet(key, out T dataLoader))
            {
                // TODO : resources
                throw new InvalidOperationException(
                    "Unable to register a DataLoader with your " +
                    "DataLoader registry.");
            }

            return dataLoader;
        }
    }
}
