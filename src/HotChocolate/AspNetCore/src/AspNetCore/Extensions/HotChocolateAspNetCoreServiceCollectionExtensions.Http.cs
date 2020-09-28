using System;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
    {
        public static IRequestExecutorBuilder AddHttpRequestInterceptor<T>(
            this IRequestExecutorBuilder builder)
            where T : class, IHttpRequestInterceptor =>
            builder.ConfigureSchemaServices(s => s
                .RemoveAll<IHttpRequestInterceptor>()
                .AddSingleton<IHttpRequestInterceptor, T>());

        public static IRequestExecutorBuilder AddHttpRequestInterceptor<T>(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, T> factory)
            where T : class, IHttpRequestInterceptor =>
            builder.ConfigureSchemaServices(s => s
                .RemoveAll<IHttpRequestInterceptor>()
                .AddSingleton<IHttpRequestInterceptor, T>(factory));

        public static IRequestExecutorBuilder AddHttpRequestInterceptor(
            this IRequestExecutorBuilder builder,
            HttpRequestInterceptorDelegate interceptor) =>
            AddHttpRequestInterceptor(
                builder,
                sp => new DelegateHttpRequestInterceptor(interceptor));

        private static IRequestExecutorBuilder AddHttpRequestInterceptor(
            this IRequestExecutorBuilder builder)
        {
            return builder.ConfigureSchemaServices(s =>
                s.TryAddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>());
        }

        public static IServiceCollection AddHttpRequestSerializer(
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

        public static IServiceCollection AddHttpRequestSerializer<T>(
            this IServiceCollection services)
            where T : class, IHttpResultSerializer
        {
            services.RemoveAll<IHttpResultSerializer>();
            services.AddSingleton<IHttpResultSerializer, T>();
            return services;
        }
    }
}
