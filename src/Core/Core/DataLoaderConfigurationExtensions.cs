using System;
using GreenDonut;
using HotChocolate.Runtime;

namespace HotChocolate
{
    public static class DataLoaderConfigurationExtensions
    {
        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, TLoader> loaderFactory)
            where TLoader : IDataLoader
        {
            throw new NotSupportedException(
                "This method is no longer supported.");
        }

        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration,
            Type type,
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, IDataLoader> loaderFactory)

        {
            throw new NotSupportedException(
                "This method is no longer supported.");
        }

        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            ExecutionScope scope,
            Func<IServiceProvider, TLoader> loaderFactory)
            where TLoader : IDataLoader
        {
            throw new NotSupportedException(
                "This method is no longer supported.");
        }

        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration,
            Type type,
            ExecutionScope scope,
            Func<IServiceProvider, IDataLoader> loaderFactory)
        {
            throw new NotSupportedException(
                "This method is no longer supported.");
        }

        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            Func<IServiceProvider, TLoader> loaderFactory)
            where TLoader : IDataLoader
        {
            throw new NotSupportedException(
                "This method is no longer supported.");
        }

        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration,
            Type type,
            Func<IServiceProvider, IDataLoader> loaderFactory)

        {
            throw new NotSupportedException(
                "This method is no longer supported.");
        }

        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            string key,
            ExecutionScope scope)
            where TLoader : class, IDataLoader
        {
            throw new NotSupportedException(
                "This method is no longer supported.");
        }

        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration,
            Type type,
            string key,
            ExecutionScope scope)

        {
            throw new NotSupportedException(
                "This method is no longer supported.");
        }

        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration,
            ExecutionScope scope)
            where TLoader : class, IDataLoader
        {
            throw new NotSupportedException(
                "This method is no longer supported.");
        }

        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration,
            Type type,
            ExecutionScope scope)

        {
            throw new NotSupportedException(
                "This method is no longer supported.");
        }

        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader<TLoader>(
            this ISchemaConfiguration configuration)
            where TLoader : class, IDataLoader
        {

            throw new NotSupportedException(
                "This method is no longer supported.");
        }

        [Obsolete("Use the DataLoaderRegistry instead. See XXX for more information.", true)]
        public static void RegisterDataLoader(
            this ISchemaConfiguration configuration, Type type)

        {
            throw new NotSupportedException(
                "This method is no longer supported.");
        }
    }
}
