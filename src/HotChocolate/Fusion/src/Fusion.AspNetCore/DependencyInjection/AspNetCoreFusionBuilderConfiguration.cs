using System.Diagnostics.CodeAnalysis;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Transport.Formatters;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

// TODO [17]: Switch the shared operations to IFusionRouterBuilder when legacy builders retire.
#pragma warning disable CS0618 // The common contract deliberately accepts legacy-only builders.
internal static class AspNetCoreFusionBuilderConfiguration
{
    public static void AddHttpRequestInterceptor<T>(IFusionGatewayBuilder builder)
        where T : IHttpRequestInterceptor, new()
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpRequestInterceptor>();
                s.AddSingleton<IHttpRequestInterceptor>(new T());
            });
    }

    public static void AddHttpRequestInterceptor(
        IFusionGatewayBuilder builder,
        Func<IServiceProvider, IHttpRequestInterceptor> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpRequestInterceptor>();
                s.AddSingleton(factory);
            });
    }

    public static void AddSocketSessionInterceptor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        IFusionGatewayBuilder builder)
        where T : class, ISocketSessionInterceptor
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<ISocketSessionInterceptor>();
                s.AddSingleton<ISocketSessionInterceptor, T>();
            });
    }

    public static void AddSocketSessionInterceptor<T>(
        IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, ISocketSessionInterceptor
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<ISocketSessionInterceptor>();
                s.AddSingleton<ISocketSessionInterceptor, T>(factory);
            });
    }

    public static void AddHttpResponseFormatter(
        IFusionGatewayBuilder builder,
        bool indented,
        IncrementalDeliveryFormat incrementalDeliveryFormat)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpResponseFormatter>();
                s.AddSingleton<IHttpResponseFormatter>(
                    sp => DefaultHttpResponseFormatter.Create(
                        new HttpResponseFormatterOptions
                        {
                            Json = new JsonResultFormatterOptions
                            {
                                Indented = indented
                            }
                        },
                        sp.GetRequiredService<ITimeProvider>(),
                        incrementalDeliveryFormat));
            });
    }

    public static void AddHttpResponseFormatter(
        IFusionGatewayBuilder builder,
        HttpResponseFormatterOptions options,
        IncrementalDeliveryFormat incrementalDeliveryFormat)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpResponseFormatter>();
                s.AddSingleton<IHttpResponseFormatter>(
                    sp => DefaultHttpResponseFormatter.Create(
                        options,
                        sp.GetRequiredService<ITimeProvider>(),
                        incrementalDeliveryFormat));
            });
    }

    public static void AddHttpResponseFormatter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        IFusionGatewayBuilder builder)
        where T : class, IHttpResponseFormatter
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpResponseFormatter>();
                s.AddSingleton<IHttpResponseFormatter, T>();
            });
    }

    public static void AddHttpResponseFormatter<T>(
        IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IHttpResponseFormatter
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpResponseFormatter>();
                s.AddSingleton<IHttpResponseFormatter>(factory);
            });
    }

    public static void ModifyServerOptions(
        IFusionGatewayBuilder builder,
        Action<GraphQLServerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure(builder.Name, configure);
    }
}
#pragma warning restore CS0618
