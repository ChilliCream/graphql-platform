using System;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Configuration;
using HotChocolate.Runtime;

namespace HotChocolate
{
    public static class DataLoaderConfigurationExtensions
    {
        public static void RegisterDataLoader<TLoader, TKey, TValue>(
            this IDataLoaderConfiguration configuration,
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, DataLoaderBase<TKey, TValue>> loaderFactory)
            where TLoader : DataLoaderBase<TKey, TValue>
        {
            configuration.RegisterDataLoader(key, scope, loaderFactory,
                (d, c) => d.DispatchAsync());
        }

        public static void RegisterDataLoader<TLoader, TKey, TValue>(
            this IDataLoaderConfiguration configuration,
            ExecutionScope scope,
            Func<IServiceProvider, DataLoaderBase<TKey, TValue>> loaderFactory)
            where TLoader : DataLoaderBase<TKey, TValue>
        {
            RegisterDataLoader<TLoader, TKey, TValue>(
                configuration, typeof(TLoader).FullName,
                scope, loaderFactory);
        }

        public static void RegisterDataLoader<TLoader, TKey, TValue>(
            this IDataLoaderConfiguration configuration,
            Func<IServiceProvider, DataLoaderBase<TKey, TValue>> loaderFactory)
            where TLoader : DataLoaderBase<TKey, TValue>
        {
            RegisterDataLoader<TLoader, TKey, TValue>(
                configuration, typeof(TLoader).FullName,
                ExecutionScope.Request, loaderFactory);
        }
    }
}
