using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.ParameterExpressionBuilders;
using HotChocolate.AspNetCore.Parsers;
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
    private static IRequestExecutorBuilder AddGraphQLServerCore(
        this IRequestExecutorBuilder builder,
        int maxAllowedRequestSize = MaxAllowedRequestSize)
    {
        builder.ConfigureSchemaServices(s =>
        {
            s.TryAddSingleton<IHttpResponseFormatter>(
                sp => DefaultHttpResponseFormatter.Create(
                    new HttpResponseFormatterOptions { HttpTransportVersion = HttpTransportVersion.Latest },
                    sp.GetRequiredService<ITimeProvider>()));
            s.TryAddSingleton<IHttpRequestParser>(
                sp => new DefaultHttpRequestParser(
                    sp.GetRequiredService<IDocumentCache>(),
                    sp.GetRequiredService<IDocumentHashProvider>(),
                    maxAllowedRequestSize,
                    sp.GetRequiredService<ParserOptions>()));

            s.TryAddSingleton<IServerDiagnosticEvents>(sp =>
            {
                var listeners = sp.GetServices<IServerDiagnosticEventListener>().ToArray();
                return listeners.Length switch
                {
                    0 => new NoopServerDiagnosticEventListener(),
                    1 => listeners[0],
                    _ => new AggregateServerDiagnosticEventListener(listeners)
                };
            });
        });

        if (!builder.Services.IsImplementationTypeRegistered<HttpContextParameterExpressionBuilder>())
        {
            builder.Services.AddSingleton<IParameterExpressionBuilder, HttpContextParameterExpressionBuilder>();
        }

        if (!builder.Services.IsImplementationTypeRegistered<HttpRequestParameterExpressionBuilder>())
        {
            builder.Services.AddSingleton<IParameterExpressionBuilder, HttpRequestParameterExpressionBuilder>();
        }

        if (!builder.Services.IsImplementationTypeRegistered<HttpResponseParameterExpressionBuilder>())
        {
            builder.Services.AddSingleton<IParameterExpressionBuilder, HttpResponseParameterExpressionBuilder>();
        }

        return builder;
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
        string? schemaName = null,
        int maxAllowedRequestSize = MaxAllowedRequestSize,
        bool disableDefaultSecurity = false)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = services
            .AddGraphQL(schemaName)
            .AddGraphQLServerCore(maxAllowedRequestSize)
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
        string? schemaName = null,
        bool disableDefaultSecurity = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Services.AddGraphQLServer(schemaName, disableDefaultSecurity: disableDefaultSecurity);
    }

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
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddType<UploadType>();
        return builder;
    }
}
