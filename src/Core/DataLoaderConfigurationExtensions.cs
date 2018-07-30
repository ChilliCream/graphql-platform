using System;
using GreenDonut;
using HotChocolate.Runtime;

namespace HotChocolate
{
    public static class DataLoaderConfigurationExtensions
    {
        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, IDispatchableDataLoader> loaderFactory)
            where TLoader : IDispatchableDataLoader
        {
            configuration.RegisterDataLoader(
                key, scope, loaderFactory,
                (d, c) => d.DispatchAsync());
        }

        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            ExecutionScope scope,
            Func<IServiceProvider, IDispatchableDataLoader> loaderFactory)
            where TLoader : IDispatchableDataLoader
        {
            RegisterDataLoader<TLoader>(
                configuration, typeof(TLoader).FullName,
                scope, loaderFactory);
        }

        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            Func<IServiceProvider, IDispatchableDataLoader> loaderFactory)
            where TLoader : IDispatchableDataLoader
        {
            RegisterDataLoader<TLoader>(
                configuration, typeof(TLoader).FullName,
                ExecutionScope.Request, loaderFactory);
        }

        public static void RegisterDataLoader<TLoader>(
           this ISchemaConfiguration configuration,
           string key,
           ExecutionScope scope)
           where TLoader : class, IDispatchableDataLoader
        {
            configuration.RegisterDataLoader<TLoader>(
                key, scope, triggerLoaderAsync:
                (d, c) => d.DispatchAsync());
        }

        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            ExecutionScope scope)
            where TLoader : class, IDispatchableDataLoader
        {
            RegisterDataLoader<TLoader>(
                configuration, typeof(TLoader).FullName,
                scope);
        }

        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration)
            where TLoader : class, IDispatchableDataLoader
        {
            RegisterDataLoader<TLoader>(
                configuration, typeof(TLoader).FullName,
                ExecutionScope.Request);
        }
    }
}
