using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Runtime;

namespace HotChocolate
{
    public static class DataLoaderConfigurationExtensions
    {
        public static void RegisterDataLoader<T>(
            this IDataLoaderConfiguration configuration,
            Func<IServiceProvider, T> loaderFactory = null,
            Func<T, CancellationToken, Task> triggerLoaderAsync = null)
        {
            RegisterDataLoader<T>(configuration, ExecutionScope.Request);
        }

        public static void RegisterDataLoader<T>(
            this IDataLoaderConfiguration configuration,
            ExecutionScope scope,
            Func<IServiceProvider, T> loaderFactory = null,
            Func<T, CancellationToken, Task> triggerLoaderAsync = null)
        {
            configuration.RegisterDataLoader<T>(
                typeof(T).FullName, scope, loaderFactory, triggerLoaderAsync);
        }

        public static void RegisterDataLoader<T>(
            this IDataLoaderConfiguration configuration,
            string key,
            Func<IServiceProvider, T> loaderFactory = null,
            Func<T, CancellationToken, Task> triggerLoaderAsync = null)
        {
            configuration.RegisterDataLoader<T>(key, ExecutionScope.Request,
                loaderFactory, triggerLoaderAsync);
        }
    }
}
