using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class AspNetCoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Adds an interceptor for GraphQL over HTTP requests.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionRouterBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="IHttpRequestInterceptor"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionRouterBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionRouterBuilder AddHttpRequestInterceptor<T>(
        this IFusionRouterBuilder builder)
        where T : IHttpRequestInterceptor, new()
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpRequestInterceptor<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds an interceptor for GraphQL over HTTP requests.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionRouterBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// A factory that creates the interceptor instance.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionRouterBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="CoreFusionRouterBuilderExtensions.AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// </remarks>
    public static IFusionRouterBuilder AddHttpRequestInterceptor(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, IHttpRequestInterceptor> factory)
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpRequestInterceptor(builder, factory);
        return builder;
    }

    /// <summary>
    /// Adds an interceptor for GraphQL socket sessions.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionRouterBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="ISocketSessionInterceptor"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionRouterBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of
    /// the schema services. If your <typeparamref name="T"/> needs to access application services
    /// you need to make the services available in the schema services via
    /// <see cref="CoreFusionRouterBuilderExtensions.AddApplicationService"/>.
    /// </remarks>
    public static IFusionRouterBuilder AddSocketSessionInterceptor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder)
        where T : class, ISocketSessionInterceptor
    {
        AspNetCoreFusionBuilderConfiguration.AddSocketSessionInterceptor<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds an interceptor for GraphQL socket sessions.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionRouterBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// A factory that creates the interceptor instance.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="ISocketSessionInterceptor"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionRouterBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="CoreFusionRouterBuilderExtensions.AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// </remarks>
    public static IFusionRouterBuilder AddSocketSessionInterceptor<T>(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, ISocketSessionInterceptor
    {
        AspNetCoreFusionBuilderConfiguration.AddSocketSessionInterceptor(builder, factory);
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="DefaultHttpResponseFormatter"/> with specific formatter options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionRouterBuilder"/>.
    /// </param>
    /// <param name="indented">
    /// Defines whether the underlying <see cref="Utf8JsonWriter"/>
    /// should pretty print the JSON which includes:
    /// indenting nested JSON tokens, adding new lines, and adding
    /// white space between property names and values.
    /// By default, the JSON is written without extra white spaces.
    /// </param>
    /// <param name="incrementalDeliveryFormat">
    /// The default incremental delivery format to use when the client does not specify one
    /// via the <c>Accept</c> header. Defaults to <see cref="IncrementalDeliveryFormat.Version_0_2"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionRouterBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionRouterBuilder AddHttpResponseFormatter(
        this IFusionRouterBuilder builder,
        bool indented = false,
        IncrementalDeliveryFormat incrementalDeliveryFormat = IncrementalDeliveryFormat.Version_0_2)
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter(builder, indented, incrementalDeliveryFormat);
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="DefaultHttpResponseFormatter"/> with specific formatter options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionRouterBuilder"/>.
    /// </param>
    /// <param name="options">
    /// The HTTP response formatter options.
    /// </param>
    /// <param name="incrementalDeliveryFormat">
    /// The default incremental delivery format to use when the client does not specify one
    /// via the <c>Accept</c> header. Defaults to <see cref="IncrementalDeliveryFormat.Version_0_2"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionRouterBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionRouterBuilder AddHttpResponseFormatter(
        this IFusionRouterBuilder builder,
        HttpResponseFormatterOptions options,
        IncrementalDeliveryFormat incrementalDeliveryFormat = IncrementalDeliveryFormat.Version_0_2)
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter(builder, options, incrementalDeliveryFormat);
        return builder;
    }

    /// <summary>
    /// Adds a custom HTTP response formatter.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionRouterBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The type of the custom <see cref="IHttpResponseFormatter"/>.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionRouterBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of
    /// the schema services. If your <typeparamref name="T"/> needs to access application services
    /// you need to make the services available in the schema services via
    /// <see cref="CoreFusionRouterBuilderExtensions.AddApplicationService"/>.
    /// </remarks>
    public static IFusionRouterBuilder AddHttpResponseFormatter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder)
        where T : class, IHttpResponseFormatter
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a custom HTTP response formatter.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionRouterBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// The service factory.
    /// </param>
    /// <typeparam name="T">
    /// The type of the custom <see cref="IHttpResponseFormatter"/>.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionRouterBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="CoreFusionRouterBuilderExtensions.AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// </remarks>
    public static IFusionRouterBuilder AddHttpResponseFormatter<T>(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IHttpResponseFormatter
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter(builder, factory);
        return builder;
    }
}
