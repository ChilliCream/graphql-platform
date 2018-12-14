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
            Func<IServiceProvider, TLoader> loaderFactory)
            where TLoader : IDispatchableDataLoader
        {
            configuration.RegisterDataLoader<TLoader>(key, scope, loaderFactory,(d, c) => d.DispatchAsync());
        }

        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration,
            Type type,
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, IDispatchableDataLoader> loaderFactory)
            
        {
            configuration.RegisterDataLoader(type,key, scope, loaderFactory,(d, c) => ((IDispatchableDataLoader)d).DispatchAsync());
        }

        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            ExecutionScope scope,
            Func<IServiceProvider, TLoader> loaderFactory)
            where TLoader : IDispatchableDataLoader
        {
            RegisterDataLoader<TLoader>(configuration, typeof(TLoader).FullName,scope, loaderFactory);
        }

        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration,
            Type type,
            ExecutionScope scope,
            Func<IServiceProvider, IDispatchableDataLoader> loaderFactory)
        {
            RegisterDataLoader(configuration, type, type.FullName, scope, loaderFactory);
        }

        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            Func<IServiceProvider, TLoader> loaderFactory)
            where TLoader : IDispatchableDataLoader
        {
            RegisterDataLoader<TLoader>(configuration, typeof(TLoader).FullName,ExecutionScope.Request, loaderFactory);
        }

        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration,
            Type type,
            Func<IServiceProvider, IDispatchableDataLoader> loaderFactory)
           
        {
            RegisterDataLoader(configuration, type,type.FullName,ExecutionScope.Request, loaderFactory);
        }

        public static void RegisterDataLoader<TLoader>(
           this ISchemaConfiguration configuration,
           string key,
           ExecutionScope scope)
           where TLoader : class, IDispatchableDataLoader
        {
            configuration.RegisterDataLoader<TLoader>(key, scope, triggerLoaderAsync:(d, c) => d.DispatchAsync());
        }

        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration,
            Type type,
            string key,
            ExecutionScope scope)
           
        {
            configuration.RegisterDataLoader(type,key, scope, triggerLoaderAsync:(d, c) => ((IDispatchableDataLoader)d).DispatchAsync());
        }

        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            ExecutionScope scope)
            where TLoader : class, IDispatchableDataLoader
        {
            RegisterDataLoader<TLoader>(configuration, typeof(TLoader).FullName,scope);
        }

        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration,
            Type type,
            ExecutionScope scope)
            
        {
            RegisterDataLoader(configuration, type, type.FullName,scope);
        }

        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration)
            where TLoader : class, IDispatchableDataLoader
        {

            RegisterDataLoader<TLoader>(configuration, typeof(TLoader).FullName,ExecutionScope.Request);
        }

        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration, Type type)
            
        {
            RegisterDataLoader(configuration,type, type.FullName,ExecutionScope.Request);
        }
    }
}
