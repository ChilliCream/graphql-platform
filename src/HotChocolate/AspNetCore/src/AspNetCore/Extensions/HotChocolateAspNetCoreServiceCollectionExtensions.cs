using HotChocolate.AspNetCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using static HotChocolate.AspNetCore.ServerDefaults;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides DI extension methods to configure a GraphQL server.
/// </summary>
public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds the GraphQL server core services to the DI.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="maxAllowedRequestSize">
    /// The max allowed GraphQL request size.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <see cref="IServiceCollection"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddGraphQLServerCore(
        this IServiceCollection services,
        int maxAllowedRequestSize = MaxAllowedRequestSize)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddGraphQLCore();
        services.TryAddSingleton<IHttpResponseFormatter>(
            new DefaultHttpResponseFormatter());
        services.TryAddSingleton<IHttpRequestParser>(
            sp => new DefaultHttpRequestParser(
                sp.GetRequiredService<IDocumentCache>(),
                sp.GetRequiredService<IDocumentHashProvider>(),
                maxAllowedRequestSize,
                sp.GetRequiredService<ParserOptions>()));
        services.TryAddSingleton<IServerDiagnosticEvents>(sp =>
        {
            var listeners =
                sp.GetServices<IServerDiagnosticEventListener>().ToArray();
            return listeners.Length switch
            {
                0 => new NoopServerDiagnosticEventListener(),
                1 => listeners[0],
                _ => new AggregateServerDiagnosticEventListener(listeners)
            };
        });

        if (services.All(t => t.ImplementationType !=
            typeof(HttpContextParameterExpressionBuilder)))
        {
            services.AddSingleton<IParameterExpressionBuilder,
                HttpContextParameterExpressionBuilder>();
        }

        return services;
    }

    /// <summary>
    /// Adds a GraphQL server configuration to the DI.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema. Use explicit schema names if you host multiple schemas.
    /// </param>
    /// <param name="maxAllowedRequestSize">
    /// The max allowed GraphQL request size.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddGraphQLServer(
        this IServiceCollection services,
        string? schemaName = default,
        int maxAllowedRequestSize = MaxAllowedRequestSize)
        => services
            .AddGraphQLServerCore(maxAllowedRequestSize)
            .AddGraphQL(schemaName)
            .AddDefaultHttpRequestInterceptor()
            .AddSubscriptionServices();

    /// <summary>
    /// Adds a GraphQL server configuration to the DI.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema. Use explicit schema names if you host multiple schemas.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddGraphQLServer(
        this IRequestExecutorBuilder builder,
        string? schemaName = default) =>
        builder.Services.AddGraphQLServer(schemaName);

    [Obsolete(
        "Use the new configuration API -> " +
        "services.AddGraphQLServer().AddQueryType<Query>()...")]
    public static IServiceCollection AddGraphQL(
        this IServiceCollection services,
        ISchema schema,
        int maxAllowedRequestSize = MaxAllowedRequestSize) =>
        RequestExecutorBuilderLegacyHelper.SetSchema(
            services
                .AddGraphQLServerCore(maxAllowedRequestSize)
                .AddGraphQL()
                .AddDefaultHttpRequestInterceptor()
                .AddSubscriptionServices(),
            schema)
            .Services;

    [Obsolete(
        "Use the new configuration API -> " +
        "services.AddGraphQLServer().AddQueryType<Query>()...")]
    public static IServiceCollection AddGraphQL(
        this IServiceCollection services,
        Func<IServiceProvider, ISchema> schemaFactory,
        int maxAllowedRequestSize = MaxAllowedRequestSize) =>
        RequestExecutorBuilderLegacyHelper.SetSchema(
                services
                    .AddGraphQLServerCore(maxAllowedRequestSize)
                    .AddGraphQL()
                    .AddDefaultHttpRequestInterceptor()
                    .AddSubscriptionServices(),
                schemaFactory)
            .Services;

    [Obsolete(
        "Use the new configuration API -> " +
        "services.AddGraphQLServer().AddQueryType<Query>()...")]
    public static IServiceCollection AddGraphQL(
        this IServiceCollection services,
        ISchemaBuilder schemaBuilder,
        int maxAllowedRequestSize = MaxAllowedRequestSize) =>
        RequestExecutorBuilderLegacyHelper.SetSchemaBuilder(
            services
                .AddGraphQLServerCore(maxAllowedRequestSize)
                .AddGraphQL()
                .AddDefaultHttpRequestInterceptor()
                .AddSubscriptionServices(),
            schemaBuilder)
            .Services;
}
