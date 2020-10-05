using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RequestExecutorBuilderExtensions
    {
        public static IServiceCollection AddIdSerializer(
            this IServiceCollection services,
            bool includeSchemaName = false)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.RemoveAll<IIdSerializer>();
            services.AddSingleton<IIdSerializer>(new IdSerializer(includeSchemaName));
            return services;
        }

        public static IServiceCollection AddIdSerializer<T>(
            this IServiceCollection services)
            where T : class, IIdSerializer
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.RemoveAll<IIdSerializer>();
            services.AddSingleton<IIdSerializer, T>();
            return services;
        }

        public static IServiceCollection AddIdSerializer(
            this IServiceCollection services,
            Func<IServiceProvider, IIdSerializer> factory)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            services.RemoveAll<IIdSerializer>();
            services.AddSingleton(factory);
            return services;
        }

        public static IRequestExecutorBuilder AddIdSerializer(
            this IRequestExecutorBuilder builder,
            bool includeSchemaName = false)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddIdSerializer(includeSchemaName);
            return builder;
        }

        public static IRequestExecutorBuilder AddIdSerializer<T>(
            this IRequestExecutorBuilder builder)
            where T : class, IIdSerializer
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddIdSerializer<T>();
            return builder;
        }

        public static IRequestExecutorBuilder AddIdSerializer(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, IIdSerializer> factory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            builder.Services.AddIdSerializer(factory);
            return builder;
        }
    }
}
