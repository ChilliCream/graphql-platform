using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GreenDonut;
using HotChocolate.DataLoader;
using HotChocolate.Properties;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate.Resolvers
{
    public static class DataLoaderResolverContextExtensions
    {
        public static IDataLoader<TKey, TValue> BatchDataLoader<TKey, TValue>(
            this IResolverContext context,
            FetchBatch<TKey, TValue> fetch,
            string? key = null)
            where TKey : notnull
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (fetch is null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            IServiceProvider services = context.Services;
            IDataLoaderRegistry reg = services.GetRequiredService<IDataLoaderRegistry>();
            Func<FetchBatchDataLoader<TKey, TValue>> createDataLoader =
                () => new FetchBatchDataLoader<TKey, TValue>(
                    services.GetRequiredService<IBatchScheduler>(),
                    fetch);

            return key is null
                ? reg.GetOrRegister(createDataLoader)
                : reg.GetOrRegister(key, createDataLoader);
        }

        [Obsolete]
        public static IDataLoader<TKey, TValue> BatchDataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchBatch<TKey, TValue> fetch)
            where TKey : notnull
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    TypeResources.DataLoaderRegistry_KeyNullOrEmpty,
                    nameof(key));
            }

            return BatchDataLoader(context, fetch, key);
        }

        public static IDataLoader<TKey, TValue[]> GroupDataLoader<TKey, TValue>(
            this IResolverContext context,
            FetchGroup<TKey, TValue> fetch,
            string? key = null)
            where TKey : notnull
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (fetch is null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            IServiceProvider services = context.Services;
            IDataLoaderRegistry reg = services.GetRequiredService<IDataLoaderRegistry>();
            Func<FetchGroupedDataLoader<TKey, TValue>> createDataLoader =
                () => new FetchGroupedDataLoader<TKey, TValue>(
                    services.GetRequiredService<IBatchScheduler>(),
                    fetch);

            return key is null
                ? reg.GetOrRegister(createDataLoader)
                : reg.GetOrRegister(key, createDataLoader);
        }

        [Obsolete]
        public static IDataLoader<TKey, TValue[]> GroupDataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchGroup<TKey, TValue> fetch)
            where TKey : notnull
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    TypeResources.DataLoaderRegistry_KeyNullOrEmpty,
                    nameof(key));
            }

            return GroupDataLoader(context, fetch, key);
        }

        public static IDataLoader<TKey, TValue> CacheDataLoader<TKey, TValue>(
            this IResolverContext context,
            FetchCacheCt<TKey, TValue> fetch,
            string? key = null,
            int cacheSize = DataLoaderDefaults.CacheSize)
            where TKey : notnull
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (fetch is null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            IServiceProvider services = context.Services;
            IDataLoaderRegistry reg = services.GetRequiredService<IDataLoaderRegistry>();
            Func<FetchCacheDataLoader<TKey, TValue>> createDataLoader =
                () => new FetchCacheDataLoader<TKey, TValue>(
                    fetch,
                    cacheSize);

            return key is null
                ? reg.GetOrRegister(createDataLoader)
                : reg.GetOrRegister(key, createDataLoader);
        }

        [Obsolete]
        public static IDataLoader<TKey, TValue> CacheDataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchCacheCt<TKey, TValue> fetch,
            int cacheSize = DataLoaderDefaults.CacheSize)
            where TKey : notnull
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    TypeResources.DataLoaderRegistry_KeyNullOrEmpty,
                    nameof(key));
            }

            return CacheDataLoader(context, fetch, key, cacheSize);
        }

        public static Task<TValue> FetchOnceAsync<TValue>(
            this IResolverContext context,
            Func<CancellationToken, Task<TValue>> fetch,
            string? key = null)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (fetch is null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return CacheDataLoader<string, TValue>(
                context,
                (k, ct) => fetch(ct),
                key,
                cacheSize: 1)
                .LoadAsync("default", context.RequestAborted);
        }

        [Obsolete]
        public static Task<TValue> FetchOnceAsync<TValue>(
            this IResolverContext context,
            string key,
            Func<CancellationToken, Task<TValue>> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    TypeResources.DataLoaderRegistry_KeyNullOrEmpty,
                    nameof(key));
            }

            return FetchOnceAsync(context, fetch, key);
        }

        public static T DataLoader<T>(this IResolverContext context, string key)
            where T : notnull, IDataLoader
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            IServiceProvider services = context.Services;
            IDataLoaderRegistry reg = services.GetRequiredService<IDataLoaderRegistry>();
            return reg.GetOrRegister(key, () => CreateDataLoader<T>(services));
        }

        public static T DataLoader<T>(this IResolverContext context)
            where T : notnull, IDataLoader
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            IServiceProvider services = context.Services;
            IDataLoaderRegistry reg = services.GetRequiredService<IDataLoaderRegistry>();
            return reg.GetOrRegister(() => CreateDataLoader<T>(services));
        }

        private static T CreateDataLoader<T>(IServiceProvider services)
            where T : IDataLoader
        {
            T registeredDataLoader = services.GetService<T>();

            if (registeredDataLoader is null)
            {
                if (typeof(T).IsInterface || typeof(T).IsAbstract)
                {
                    throw new RegisterDataLoaderException(
                        string.Format(
                            DataLoaderResolverContextExtensions_CreateDataLoader_AbstractType,
                            typeof(T).FullName ?? typeof(T).Name));
                }

                var factory = new ServiceFactory { Services = services };
                if (factory.CreateInstance(typeof(T)) is T dataLoader)
                {
                    return dataLoader;
                }

                throw new RegisterDataLoaderException(
                    string.Format(
                        DataLoaderResolverContextExtensions_CreateDataLoader_UnableToCreate,
                        typeof(T).FullName ?? typeof(T).Name));
            }

            return registeredDataLoader;
        }
    }
}
