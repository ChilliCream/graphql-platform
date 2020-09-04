using System;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
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

        public static IRequestExecutorBuilder AddHttpRequestInterceptor<T>(
            this IRequestExecutorBuilder builder,
            HttpRequestInterceptorDelegate interceptor)
            where T : class, IHttpRequestInterceptor =>
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
