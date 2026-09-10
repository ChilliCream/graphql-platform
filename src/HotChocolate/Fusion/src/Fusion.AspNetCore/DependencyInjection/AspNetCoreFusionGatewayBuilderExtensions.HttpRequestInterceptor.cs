using System.Diagnostics.CodeAnalysis;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

// TODO [17]: Remove the legacy extension surface after the 16.x compatibility window.
[Obsolete("Use AspNetCoreFusionRouterBuilderExtensions instead.")]
public static partial class AspNetCoreFusionGatewayBuilderExtensions
{
    /// <inheritdoc cref="AspNetCoreFusionRouterBuilderExtensions.AddHttpRequestInterceptor{T}(IFusionRouterBuilder)"/>
    [Obsolete("Use AddHttpRequestInterceptor on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpRequestInterceptor<T>(
        this IFusionGatewayBuilder builder)
        where T : IHttpRequestInterceptor, new()
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpRequestInterceptor<T>(builder);
        return builder;
    }

    /// <inheritdoc cref="AspNetCoreFusionRouterBuilderExtensions.AddHttpRequestInterceptor(IFusionRouterBuilder, Func{IServiceProvider, IHttpRequestInterceptor})"/>
    [Obsolete("Use AddHttpRequestInterceptor on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpRequestInterceptor(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, IHttpRequestInterceptor> factory)
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpRequestInterceptor(builder, factory);
        return builder;
    }

    /// <inheritdoc cref="AspNetCoreFusionRouterBuilderExtensions.AddSocketSessionInterceptor{T}(IFusionRouterBuilder)"/>
    [Obsolete("Use AddSocketSessionInterceptor on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddSocketSessionInterceptor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, ISocketSessionInterceptor
    {
        AspNetCoreFusionBuilderConfiguration.AddSocketSessionInterceptor<T>(builder);
        return builder;
    }

    /// <inheritdoc cref="AspNetCoreFusionRouterBuilderExtensions.AddSocketSessionInterceptor{T}(IFusionRouterBuilder, Func{IServiceProvider, T})"/>
    [Obsolete("Use AddSocketSessionInterceptor on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddSocketSessionInterceptor<T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, ISocketSessionInterceptor
    {
        AspNetCoreFusionBuilderConfiguration.AddSocketSessionInterceptor(builder, factory);
        return builder;
    }

    /// <inheritdoc cref="AspNetCoreFusionRouterBuilderExtensions.AddHttpResponseFormatter(IFusionRouterBuilder, bool, IncrementalDeliveryFormat)"/>
    [Obsolete("Use AddHttpResponseFormatter on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpResponseFormatter(
        this IFusionGatewayBuilder builder,
        bool indented = false,
        IncrementalDeliveryFormat incrementalDeliveryFormat = IncrementalDeliveryFormat.Version_0_2)
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter(builder, indented, incrementalDeliveryFormat);
        return builder;
    }

    /// <inheritdoc cref="AspNetCoreFusionRouterBuilderExtensions.AddHttpResponseFormatter(IFusionRouterBuilder, HttpResponseFormatterOptions, IncrementalDeliveryFormat)"/>
    [Obsolete("Use AddHttpResponseFormatter on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpResponseFormatter(
        this IFusionGatewayBuilder builder,
        HttpResponseFormatterOptions options,
        IncrementalDeliveryFormat incrementalDeliveryFormat = IncrementalDeliveryFormat.Version_0_2)
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter(builder, options, incrementalDeliveryFormat);
        return builder;
    }

    /// <inheritdoc cref="AspNetCoreFusionRouterBuilderExtensions.AddHttpResponseFormatter{T}(IFusionRouterBuilder)"/>
    [Obsolete("Use AddHttpResponseFormatter on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpResponseFormatter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IHttpResponseFormatter
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter<T>(builder);
        return builder;
    }

    /// <inheritdoc cref="AspNetCoreFusionRouterBuilderExtensions.AddHttpResponseFormatter{T}(IFusionRouterBuilder, Func{IServiceProvider, T})"/>
    [Obsolete("Use AddHttpResponseFormatter on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpResponseFormatter<T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IHttpResponseFormatter
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter(builder, factory);
        return builder;
    }
}
