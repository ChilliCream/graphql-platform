using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an interceptor for GraphQL requests to the GraphQL configuration.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <typeparam name="T">
        /// The <see cref="IHttpRequestInterceptor"/> implementation.
        /// </typeparam>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
        /// </returns>
        public static IRequestExecutorBuilder AddHttpRequestInterceptor<T>(
            this IRequestExecutorBuilder builder)
            where T : class, IHttpRequestInterceptor =>
            builder.ConfigureSchemaServices(s => s
                .RemoveAll<IHttpRequestInterceptor>()
                .AddSingleton<IHttpRequestInterceptor, T>());

        /// <summary>
        /// Adds an interceptor for GraphQL requests to the GraphQL configuration.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="factory">
        /// A factory that creates the interceptor instance.
        /// </param>
        /// <typeparam name="T">
        /// The <see cref="IHttpRequestInterceptor"/> implementation.
        /// </typeparam>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
        /// </returns>
        public static IRequestExecutorBuilder AddHttpRequestInterceptor<T>(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, T> factory)
            where T : class, IHttpRequestInterceptor =>
            builder.ConfigureSchemaServices(s => s
                .RemoveAll<IHttpRequestInterceptor>()
                .AddSingleton<IHttpRequestInterceptor, T>(factory));

        /// <summary>
        /// Adds an interceptor for GraphQL requests to the GraphQL configuration.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="interceptor">
        /// The interceptor instance that shall be added to the configuration.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
        /// </returns>
        public static IRequestExecutorBuilder AddHttpRequestInterceptor(
            this IRequestExecutorBuilder builder,
            HttpRequestInterceptorDelegate interceptor) =>
            AddHttpRequestInterceptor(
                builder,
                _ => new DelegateHttpRequestInterceptor(interceptor));

        private static IRequestExecutorBuilder AddDefaultHttpRequestInterceptor(
            this IRequestExecutorBuilder builder)
        {
            return builder.ConfigureSchemaServices(
                s => s.TryAddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>());
        }

        /// <summary>
        /// Adds the <see cref="DefaultHttpResultSerializer"/> with specific serialization settings
        /// to the DI.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="batchSerialization">
        /// Specifies the batch serialization format.
        /// </param>
        /// <param name="deferSerialization"></param>
        /// Specifies the defer/stream serialization format.
        /// <returns>
        /// Returns the <see cref="IServiceCollection"/> so that configuration can be chained.
        /// </returns>
        public static IServiceCollection AddHttpResultSerializer(
            this IServiceCollection services,
            HttpResultSerialization batchSerialization = HttpResultSerialization.MultiPartChunked,
            HttpResultSerialization deferSerialization = HttpResultSerialization.MultiPartChunked)
        {
            services.RemoveAll<IHttpResultSerializer>();
            services.AddSingleton<IHttpResultSerializer>(
                new DefaultHttpResultSerializer(
                    batchSerialization,
                    deferSerialization));
            return services;
        }

        /// <summary>
        /// Adds a custom http request serializer to the DI.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <typeparam name="T">
        /// The type of the custom <see cref="IHttpResultSerializer"/>.
        /// </typeparam>
        /// <returns>
        /// Returns the <see cref="IServiceCollection"/> so that configuration can be chained.
        /// </returns>
        public static IServiceCollection AddHttpResultSerializer<T>(
            this IServiceCollection services)
            where T : class, IHttpResultSerializer
        {
            services.RemoveAll<IHttpResultSerializer>();
            services.AddSingleton<IHttpResultSerializer, T>();
            return services;
        }
    }
}
