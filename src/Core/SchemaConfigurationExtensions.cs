using System;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Configuration;
using HotChocolate.Runtime;

namespace HotChocolate
{
    public static class SchemaConfigurationExtensions
    {
        public static void RegisterLoader<TLoader, TKey, TValue>(
            this IDataLoaderConfiguration configuration,
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, DataLoaderBase<TKey, TValue>> loaderFactory)
            where TLoader : DataLoaderBase<TKey, TValue>
        {
            Func<DataLoaderBase<TKey, TValue>, CancellationToken, Task> trig =
                (d, c) =>
                {
                    d.InterruptDelay();
                    return Task.CompletedTask;
                };

            configuration.RegisterLoader(key, scope, loaderFactory, trig);
        }

        public static void RegisterLoader<TLoader, TKey, TValue>(
           this IDataLoaderConfiguration configuration,
           ExecutionScope scope,
           Func<IServiceProvider, DataLoaderBase<TKey, TValue>> loaderFactory)
           where TLoader : DataLoaderBase<TKey, TValue>
        {
            RegisterLoader<TLoader, TKey, TValue>(
                configuration, typeof(TLoader).FullName,
                scope, loaderFactory);
        }

        public static void RegisterLoader<TLoader, TKey, TValue>(
            this IDataLoaderConfiguration configuration,
            Func<IServiceProvider, DataLoaderBase<TKey, TValue>> loaderFactory)
            where TLoader : DataLoaderBase<TKey, TValue>
        {
            RegisterLoader<TLoader, TKey, TValue>(
                configuration, typeof(TLoader).FullName,
                ExecutionScope.Request, loaderFactory);
        }
    }
}
