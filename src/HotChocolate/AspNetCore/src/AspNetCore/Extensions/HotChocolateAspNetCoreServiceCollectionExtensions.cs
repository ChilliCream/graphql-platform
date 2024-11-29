using HotChocolate.AspNetCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.ParameterExpressionBuilders;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Microsoft.Extensions.Hosting;
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
            sp => DefaultHttpResponseFormatter.Create(
                new HttpResponseFormatterOptions { HttpTransportVersion = HttpTransportVersion.Latest },
                sp.GetRequiredService<ITimeProvider>()));
        services.TryAddSingleton<IHttpRequestParser>(
            sp => new DefaultHttpRequestParser(
                sp.GetRequiredService<IDocumentCache>(),
                sp.GetRequiredService<IDocumentHashProvider>(),
                maxAllowedRequestSize,
                sp.GetRequiredService<ParserOptions>()));
        services.TryAddSingleton<IServerDiagnosticEvents>(sp =>
        {
            var listeners = sp.GetServices<IServerDiagnosticEventListener>().ToArray();
            return listeners.Length switch
            {
                0 => new NoopServerDiagnosticEventListener(),
                1 => listeners[0],
                _ => new AggregateServerDiagnosticEventListener(listeners),
            };
        });

        if (!services.IsImplementationTypeRegistered<HttpContextParameterExpressionBuilder>())
        {
            services.AddSingleton<IParameterExpressionBuilder, HttpContextParameterExpressionBuilder>();
        }

        if (!services.IsImplementationTypeRegistered<HttpRequestParameterExpressionBuilder>())
        {
            services.AddSingleton<IParameterExpressionBuilder, HttpRequestParameterExpressionBuilder>();
        }

        if (!services.IsImplementationTypeRegistered<HttpResponseParameterExpressionBuilder>())
        {
            services.AddSingleton<IParameterExpressionBuilder, HttpResponseParameterExpressionBuilder>();
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
    /// <param name="disableDefaultSecurity">
    /// Defines if the default security policy should be disabled.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddGraphQLServer(
        this IServiceCollection services,
        string? schemaName = default,
        int maxAllowedRequestSize = MaxAllowedRequestSize,
        bool disableDefaultSecurity = false)
    {
        var builder = services
            .AddGraphQLServerCore(maxAllowedRequestSize)
            .AddGraphQL(schemaName)
            .AddDefaultHttpRequestInterceptor()
            .AddSubscriptionServices();

        if (!disableDefaultSecurity)
        {
            builder.AddCostAnalyzer();
            builder.DisableIntrospection(
                (sp, _) =>
                {
                    var environment = sp.GetService<IHostEnvironment>();
                    return environment?.IsDevelopment() == false;
                });
            builder.AddMaxAllowedFieldCycleDepthRule(
                isEnabled: (sp, _) =>
                {
                    var environment = sp.GetService<IHostEnvironment>();
                    return environment?.IsDevelopment() == false;
                });
        }

        return builder;
    }

    /// <summary>
    /// Adds a GraphQL server configuration to the DI.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema. Use explicit schema names if you host multiple schemas.
    /// </param>
    /// <param name="disableDefaultSecurity">
    /// Defines if the default security policy should be disabled.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddGraphQLServer(
        this IRequestExecutorBuilder builder,
        string? schemaName = default,
        bool disableDefaultSecurity = false)
        => builder.Services.AddGraphQLServer(schemaName, disableDefaultSecurity: disableDefaultSecurity);

    /// <summary>
    /// Registers the GraphQL Upload Scalar.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder for configuration chaining.
    /// </returns>
    public static IRequestExecutorBuilder AddUploadType(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddType<UploadType>();
        return builder;
    }
}
