using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Serialization;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

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
            .AddSingleton<IHttpRequestInterceptor, T>(
                sp => ActivatorUtilities.GetServiceOrCreateInstance<T>(sp.GetCombinedServices())));

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
            .AddSingleton<IHttpRequestInterceptor, T>(sp => factory(sp.GetCombinedServices())));

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
    /// Adds the <see cref="DefaultHttpResponseFormatter"/> with specific formatter options
    /// to the DI.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="indented">
    /// Defines whether the underlying <see cref="Utf8JsonWriter"/>
    /// should pretty print the JSON which includes:
    /// indenting nested JSON tokens, adding new lines, and adding
    /// white space between property names and values.
    /// By default, the JSON is written without extra white spaces.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IServiceCollection"/> so that configuration can be chained.
    /// </returns>
    public static IServiceCollection AddHttpResponseFormatter(
        this IServiceCollection services,
        bool indented = false)
    {
        services.RemoveAll<IHttpResponseFormatter>();
        services.AddSingleton<IHttpResponseFormatter>(
            sp => DefaultHttpResponseFormatter.Create(
                new HttpResponseFormatterOptions
                {
                    Json = new JsonResultFormatterOptions
                    {
                        Indented = indented,
                    },
                },
                sp.GetRequiredService<ITimeProvider>()));
        return services;
    }

    /// <summary>
    /// Adds the <see cref="DefaultHttpResponseFormatter"/> with specific formatter options
    /// to the DI.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="options">
    /// The HTTP response formatter options
    /// </param>
    /// <returns>
    /// Returns the <see cref="IServiceCollection"/> so that configuration can be chained.
    /// </returns>
    public static IServiceCollection AddHttpResponseFormatter(
        this IServiceCollection services,
        HttpResponseFormatterOptions options)
    {
        services.RemoveAll<IHttpResponseFormatter>();
        services.AddSingleton<IHttpResponseFormatter>(
            sp => DefaultHttpResponseFormatter.Create(
                options,
                sp.GetRequiredService<ITimeProvider>()));
        return services;
    }

    /// <summary>
    /// Adds a custom HTTP response formatter to the DI.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <typeparam name="T">
    /// The type of the custom <see cref="IHttpResponseFormatter"/>.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IServiceCollection"/> so that configuration can be chained.
    /// </returns>
    public static IServiceCollection AddHttpResponseFormatter<T>(
        this IServiceCollection services)
        where T : class, IHttpResponseFormatter
    {
        services.RemoveAll<IHttpResponseFormatter>();
        services.AddSingleton<IHttpResponseFormatter, T>();
        return services;
    }

    /// <summary>
    /// Adds a custom HTTP response formatter to the DI.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="factory">
    /// The service factory.
    /// </param>
    /// <typeparam name="T">
    /// The type of the custom <see cref="IHttpResponseFormatter"/>.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IServiceCollection"/> so that configuration can be chained.
    /// </returns>
    public static IServiceCollection AddHttpResponseFormatter<T>(
        this IServiceCollection services,
        Func<IServiceProvider, T> factory)
        where T : class, IHttpResponseFormatter
    {
        services.RemoveAll<IHttpResponseFormatter>();
        services.AddSingleton<IHttpResponseFormatter>(factory);
        return services;
    }
}
