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
    }
}
