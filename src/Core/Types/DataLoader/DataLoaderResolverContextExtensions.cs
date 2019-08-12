using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.DataLoader;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers
{
    public static class DataLoaderResolverContextExtensions
    {
        private static IDataLoader<TKey, TValue> BatchDataLoaderFactory<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchBatchFactory<TKey, TValue> factory)
        {
            if (TryGetDataLoader(context, key,
                out IDataLoader<TKey, TValue> dataLoader,
                out IDataLoaderRegistry registry))
            {
                return dataLoader;
            }

            return GetOrCreate<IDataLoader<TKey, TValue>>(
                key, registry, r => r.Register(key, factory));
        }

        public static IDataLoader<TKey, TValue> BatchDataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchBatch<TKey, TValue> fetch)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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

            return BatchDataLoaderFactory(context, key, services => fetch);
        }

        public static IDataLoader<TKey, TValue> BatchDataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchBatchCt<TKey, TValue> fetch)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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

            return BatchDataLoader<TKey, TValue>(
                context,
                key,
                keys => fetch(keys, context.RequestAborted));
        }

        private static IDataLoader<TKey, TValue[]> GroupDataLoaderFactory<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchGroupeFactory<TKey, TValue> factory)
        {
            if (TryGetDataLoader(context, key,
                out IDataLoader<TKey, TValue[]> dataLoader,
                out IDataLoaderRegistry registry))
            {
                return dataLoader;
            }

            return GetOrCreate<IDataLoader<TKey, TValue[]>>(
                key, registry, r => r.Register(key, factory));
        }

        public static IDataLoader<TKey, TValue[]> GroupDataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchGroup<TKey, TValue> fetch)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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

            return GroupDataLoaderFactory(context, key, services => fetch);
        }

        public static IDataLoader<TKey, TValue[]> GroupDataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchGroupCt<TKey, TValue> fetch)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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

            return GroupDataLoader<TKey, TValue>(
                context,
                key,
                keys => fetch(keys, context.RequestAborted));
        }

        private static IDataLoader<TKey, TValue> CacheDataLoaderFactory<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchCacheFactory<TKey, TValue> factory)
        {
            if (TryGetDataLoader(context, key,
                out IDataLoader<TKey, TValue> dataLoader,
                out IDataLoaderRegistry registry))
            {
                return dataLoader;
            }

            return GetOrCreate<IDataLoader<TKey, TValue>>(
                key, registry, r => r.Register(key, factory));
        }

        public static IDataLoader<TKey, TValue> CacheDataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchCache<TKey, TValue> fetch)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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

            return CacheDataLoaderFactory(context, key, services => fetch);
        }

        public static IDataLoader<TKey, TValue> CacheDataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchCacheCt<TKey, TValue> fetch)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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

            return CacheDataLoader<TKey, TValue>(
                context,
                key,
                keys => fetch(keys, context.RequestAborted));
        }

        private static Func<Task<TValue>> FetchOnceFactory<TValue>(
            this IResolverContext context,
            string key,
            FetchOnceFactory<TValue> factory)
        {
            if (!TryGetDataLoader(context, key,
                out IDataLoader<string, TValue> dataLoader,
                out IDataLoaderRegistry registry))
            {
                dataLoader = GetOrCreate<IDataLoader<string, TValue>>(
                    key, registry, r => r.Register(key, factory));
            }

            return () => dataLoader.LoadAsync("none");
        }

        public static Task<TValue> FetchOnceAsync<TValue>(
            this IResolverContext context,
            string key,
            FetchOnce<TValue> fetch)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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

            return FetchOnceFactory(context, key, services => fetch)();
        }

        public static Task<TValue> FetchOnceAsync<TValue>(
            this IResolverContext context,
            string key,
            FetchOnceCt<TValue> fetch)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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

            return FetchOnceAsync(
                context,
                key,
                () => fetch(context.RequestAborted));
        }

        public static T DataLoader<T>(
            this IResolverContext context,
            string key)
            where T : class, IDataLoader
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    TypeResources.DataLoaderRegistry_KeyNullOrEmpty,
                    nameof(key));
            }

            if (TryGetDataLoader(context, key,
                out T dataLoader,
                out IDataLoaderRegistry registry))
            {
                return dataLoader;
            }

            return GetOrCreate<T>
            (
                key,
                registry,
                r => r.Register(key, services =>
                {
                    var instance = (T)services.GetService(typeof(T));
                    return instance is null
                        ? ActivatorHelper.CompileFactory<T>().Invoke(services)
                        : instance;
                })
            );
        }

        public static T DataLoader<T>(
            this IResolverContext context)
            where T : class, IDataLoader =>
            DataLoader<T>(context, typeof(T).FullName);

        private static bool TryGetDataLoader<T>(
            IResolverContext context,
            string key,
            out T dataLoader,
            out IDataLoaderRegistry registry)
            where T : IDataLoader
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

            dataLoader = default;
            return false;
        }

        private static T GetOrCreate<T>(
            string key,
            IDataLoaderRegistry registry,
            Action<IDataLoaderRegistry> register)
            where T : IDataLoader
        {
            if (registry == null)
            {
                throw new InvalidOperationException(TypeResources
                    .DataLoaderResolverContextExtensions_RegistryIsNull);
            }

            if (!registry.TryGet(key, out T dataLoader))
            {
                register(registry);

                if (!registry.TryGet(key, out dataLoader))
                {
                    throw new InvalidOperationException(TypeResources
                        .DataLoaderResolverContextExtensions_UnableToRegister);
                }
            }

            return dataLoader;
        }
    }
}
