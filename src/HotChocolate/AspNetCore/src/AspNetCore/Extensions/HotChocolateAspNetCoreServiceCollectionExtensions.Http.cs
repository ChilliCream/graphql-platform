using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.Execution.Configuration;
using HotChocolate.Transport.Formatters;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds an interceptor for GraphQL over HTTP requests to the GraphQL configuration.
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
        where T : class, IHttpRequestInterceptor
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchemaServices(s => s
            .RemoveAll<IHttpRequestInterceptor>()
            .AddSingleton<IHttpRequestInterceptor, T>(sp =>
                ActivatorUtilities.GetServiceOrCreateInstance<T>(sp.GetCombinedServices())));
    }

    /// <summary>
    /// Adds an interceptor for GraphQL over HTTP  requests to the GraphQL configuration.
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
        where T : class, IHttpRequestInterceptor
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchemaServices(s => s
            .RemoveAll<IHttpRequestInterceptor>()
            .AddSingleton<IHttpRequestInterceptor, T>(sp => factory(sp.GetCombinedServices())));
    }

    /// <summary>
    /// Adds an interceptor for GraphQL over HTTP requests to the GraphQL configuration.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="handler">
    /// A delegate that allows to configure the GraphQL request.
    /// </param>
    /// <returns></returns>
    public static IRequestExecutorBuilder AddHttpRequestInterceptor(
        this IRequestExecutorBuilder builder,
        Func<HttpContext, IRequestExecutor, OperationRequestBuilder, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddHttpRequestInterceptor(_ => new DelegateHttpRequestInterceptor(handler));
    }

    private static IRequestExecutorBuilder AddDefaultHttpRequestInterceptor(
        this IRequestExecutorBuilder builder)
        => builder.ConfigureSchemaServices(
            s => s.TryAddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>());

    /// <summary>
    /// Adds the <see cref="DefaultHttpResponseFormatter"/> with specific formatter options
    /// to the DI.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="indented">
    /// Defines whether the underlying <see cref="Utf8JsonWriter"/>
    /// should pretty print the JSON which includes:
    /// indenting nested JSON tokens, adding new lines, and adding
    /// white space between property names and values.
    /// By default, the JSON is written without extra white spaces.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddHttpResponseFormatter(
        this IRequestExecutorBuilder builder,
        bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices(
            s => s.RemoveAll<IHttpResponseFormatter>()
                .AddSingleton<IHttpResponseFormatter>(
                    sp => DefaultHttpResponseFormatter.Create(
                        new HttpResponseFormatterOptions
                        {
                            Json = new JsonResultFormatterOptions
                            {
                                Indented = indented
                            }
                        },
                        sp.GetRequiredService<ITimeProvider>())));
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="DefaultHttpResponseFormatter"/> with specific formatter options
    /// to the DI.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="options">
    /// The HTTP response formatter options
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddHttpResponseFormatter(
        this IRequestExecutorBuilder builder,
        HttpResponseFormatterOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices(
            s => s.RemoveAll<IHttpResponseFormatter>()
                .AddSingleton<IHttpResponseFormatter>(
                    sp => DefaultHttpResponseFormatter.Create(
                    options,
                    sp.GetRequiredService<ITimeProvider>())));
        return builder;
    }

    /// <summary>
    /// Adds a custom HTTP response formatter to the DI.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The type of the custom <see cref="IHttpResponseFormatter"/>.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IServiceCollection"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddHttpResponseFormatter<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IHttpResponseFormatter
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices(
            s => s.RemoveAll<IHttpResponseFormatter>()
                .AddSingleton<IHttpResponseFormatter, T>());
        return builder;
    }

    /// <summary>
    /// Adds a custom HTTP response formatter to the DI.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    public static IRequestExecutorBuilder AddHttpResponseFormatter<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IHttpResponseFormatter
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.ConfigureSchemaServices(
            s => s.RemoveAll<IHttpResponseFormatter>()
                .AddSingleton<IHttpResponseFormatter>(factory));
        return builder;
    }
}
