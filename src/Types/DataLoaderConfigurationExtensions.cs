using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Runtime;

namespace HotChocolate
{
    public static class DataLoaderConfigurationExtensions
    {
        public static void RegisterLoader<T>(
            this IDataLoaderConfiguration configuration,
            Func<IServiceProvider, T> loaderFactory = null,
            Func<T, CancellationToken, Task> triggerLoaderAsync = null)
        {
            RegisterLoader<T>(configuration, ExecutionScope.Request);
        }

        public static void RegisterLoader<T>(
            this IDataLoaderConfiguration configuration,
            ExecutionScope scope,
            Func<IServiceProvider, T> loaderFactory = null,
            Func<T, CancellationToken, Task> triggerLoaderAsync = null)
        {
            configuration.RegisterLoader<T>(
                typeof(T).FullName, scope, loaderFactory, triggerLoaderAsync);
        }

        public static void RegisterLoader<T>(
            this IDataLoaderConfiguration configuration,
            string key,
            Func<IServiceProvider, T> loaderFactory = null,
            Func<T, CancellationToken, Task> triggerLoaderAsync = null)
        {
            configuration.RegisterLoader<T>(key, ExecutionScope.Request,
                loaderFactory, triggerLoaderAsync);
        }
    }
}
