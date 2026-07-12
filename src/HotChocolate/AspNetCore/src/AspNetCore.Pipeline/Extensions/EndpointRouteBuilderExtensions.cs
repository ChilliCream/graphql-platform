using System.Diagnostics.CodeAnalysis;
using ChilliCream.Nitro.App;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory;
using MiddlewareFactory = HotChocolate.AspNetCore.MiddlewareFactory;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides GraphQL extensions to the <see cref="IEndpointConventionBuilder"/>.
/// </summary>
#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public static class EndpointRouteBuilderExtensions
{
    private const string GraphQLHttpPath = "/graphql";
    private const string GraphQLWebSocketPath = "/graphql/ws";
    private const string GraphQLSchemaPath = "/graphql/sdl";
    private const string GraphQLSemanticNonNullSchemaPath = "/graphql/semantic-non-null-schema.graphql";
    private const string GraphQLToolPath = "/graphql/ui";
    private const string GraphQLPersistedOperationPath = "/graphql/persisted";
    private const string GraphQLToolRelativeRequestPath = "..";

    /// <summary>
    /// Adds a GraphQL endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="path">
    /// The path to which the GraphQL endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static GraphQLEndpointConventionBuilder MapGraphQL(
        this IEndpointRouteBuilder endpointRouteBuilder,
        string path = GraphQLHttpPath,
        string? schemaName = null)
        => MapGraphQL(endpointRouteBuilder, new PathString(path), schemaName);

    /// <summary>
    /// Adds a GraphQL endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="path">
    /// The path to which the GraphQL endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static GraphQLEndpointConventionBuilder MapGraphQL(
        this IEndpointRouteBuilder endpointRouteBuilder,
        PathString path,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);

        TryResolveSchemaName(endpointRouteBuilder.ServiceProvider, ref schemaName);

        path = path.ToString().TrimEnd('/');
        var schemaNameOrDefault = schemaName ?? ISchemaDefinition.DefaultName;
        var pattern = Parse(path + "/{**slug}");
        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
        var services = endpointRouteBuilder.ServiceProvider;
        requestPipeline.MapGraphQL(path, schemaNameOrDefault);
        var serverOptions = services.GetServerOptions(schemaNameOrDefault);

        return new GraphQLEndpointConventionBuilder(
            endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL Pipeline")
                .WithMetadata(serverOptions.Tool));
    }

    /// <summary>
    /// Adds a GraphQL endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The <see cref="IApplicationBuilder"/>.
    /// </param>
    /// <param name="path">
    /// The path to which the GraphQL endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="applicationBuilder" /> is <c>null</c>.
    /// </exception>
    public static IApplicationBuilder MapGraphQL(
        this IApplicationBuilder applicationBuilder,
        PathString path,
        string schemaName)
    {
        ArgumentNullException.ThrowIfNull(applicationBuilder);
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        path = path.ToString().TrimEnd('/');

        var services = applicationBuilder.ApplicationServices;
        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = services.GetRequiredService<IRequestExecutorEvents>();
        var formOptions = services.GetRequiredService<IOptions<FormOptions>>();
        var executor = new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaName);
        var serverOptions = services.GetServerOptions(schemaName);

        applicationBuilder
            .Use(MiddlewareFactory.CreateCancellationMiddleware())
            .Use(MiddlewareFactory.CreateWebSocketSubscriptionMiddleware(executor, serverOptions))
            .Use(MiddlewareFactory.CreateHttpPostMiddleware(executor, serverOptions))
            .Use(MiddlewareFactory.CreateHttpMultipartMiddleware(executor, serverOptions, formOptions))
            .Use(MiddlewareFactory.CreateHttpGetMiddleware(executor, serverOptions))
            .Use(MiddlewareFactory.CreateHttpGetSchemaMiddleware(
                executor, serverOptions, path, MiddlewareRoutingType.Integrated))
            .UseNitroApp(path, serverOptions.Tool)
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        return applicationBuilder;
    }

    /// <summary>
    /// Adds a GraphQL HTTP endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL HTTP endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static GraphQLHttpEndpointConventionBuilder MapGraphQLHttp(
        this IEndpointRouteBuilder endpointRouteBuilder,
        [StringSyntax("Route")] string pattern = GraphQLHttpPath,
        string? schemaName = null)
        => MapGraphQLHttp(endpointRouteBuilder, Parse(pattern), schemaName);

    /// <summary>
    /// Adds a GraphQL HTTP endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL HTTP endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static GraphQLHttpEndpointConventionBuilder MapGraphQLHttp(
        this IEndpointRouteBuilder endpointRouteBuilder,
        RoutePattern pattern,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);
        ArgumentNullException.ThrowIfNull(pattern);

        TryResolveSchemaName(endpointRouteBuilder.ServiceProvider, ref schemaName);

        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
        var schemaNameOrDefault = schemaName ?? ISchemaDefinition.DefaultName;

        var services = endpointRouteBuilder.ServiceProvider;
        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = services.GetRequiredService<IRequestExecutorEvents>();
        var formOptions = services.GetRequiredService<IOptions<FormOptions>>();
        var executor = new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaNameOrDefault);
        var serverOptions = services.GetServerOptions(schemaNameOrDefault);

        requestPipeline
            .Use(MiddlewareFactory.CreateCancellationMiddleware())
            .Use(MiddlewareFactory.CreateHttpPostMiddleware(executor, serverOptions))
            .Use(MiddlewareFactory.CreateHttpMultipartMiddleware(executor, serverOptions, formOptions))
            .Use(MiddlewareFactory.CreateHttpGetMiddleware(executor, serverOptions))
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        return new GraphQLHttpEndpointConventionBuilder(
            endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL HTTP Pipeline"));
    }

    /// <summary>
    /// Adds a GraphQL WebSocket endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL WebSocket endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static GraphQLWebSocketEndpointConventionBuilder MapGraphQLWebSocket(
        this IEndpointRouteBuilder endpointRouteBuilder,
        [StringSyntax("Route")] string pattern = GraphQLWebSocketPath,
        string? schemaName = null)
        => MapGraphQLWebSocket(endpointRouteBuilder, Parse(pattern), schemaName);

    /// <summary>
    /// Adds a GraphQL WebSocket endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL WebSocket endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static GraphQLWebSocketEndpointConventionBuilder MapGraphQLWebSocket(
        this IEndpointRouteBuilder endpointRouteBuilder,
        RoutePattern pattern,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);
        ArgumentNullException.ThrowIfNull(pattern);

        TryResolveSchemaName(endpointRouteBuilder.ServiceProvider, ref schemaName);

        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
        var schemaNameOrDefault = schemaName ?? ISchemaDefinition.DefaultName;

        var services = endpointRouteBuilder.ServiceProvider;
        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = services.GetRequiredService<IRequestExecutorEvents>();
        var executor = new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaNameOrDefault);
        var serverOptions = services.GetServerOptions(schemaNameOrDefault);

        requestPipeline
            .Use(MiddlewareFactory.CreateCancellationMiddleware())
            .Use(MiddlewareFactory.CreateWebSocketSubscriptionMiddleware(executor, serverOptions))
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        var builder = new GraphQLEndpointConventionBuilder(
            endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL WebSocket Pipeline"));

        return new GraphQLWebSocketEndpointConventionBuilder(builder);
    }

    /// <summary>
    /// Adds a GraphQL schema SDL endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL schema SDL endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static IEndpointConventionBuilder MapGraphQLSchema(
        this IEndpointRouteBuilder endpointRouteBuilder,
        [StringSyntax("Route")] string pattern = GraphQLSchemaPath,
        string? schemaName = null)
        => MapGraphQLSchema(endpointRouteBuilder, Parse(pattern), schemaName);

    /// <summary>
    /// Adds a GraphQL schema SDL endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL schema SDL endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static IEndpointConventionBuilder MapGraphQLSchema(
        this IEndpointRouteBuilder endpointRouteBuilder,
        RoutePattern pattern,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);
        ArgumentNullException.ThrowIfNull(pattern);

        TryResolveSchemaName(endpointRouteBuilder.ServiceProvider, ref schemaName);

        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
        var schemaNameOrDefault = schemaName ?? ISchemaDefinition.DefaultName;

        var services = endpointRouteBuilder.ServiceProvider;
        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = services.GetRequiredService<IRequestExecutorEvents>();
        var executor = new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaNameOrDefault);
        var serverOptions = services.GetServerOptions(schemaNameOrDefault);

        requestPipeline
            .Use(MiddlewareFactory.CreateCancellationMiddleware())
            .Use(MiddlewareFactory.CreateHttpGetSchemaMiddleware(
                executor, serverOptions, PathString.Empty, MiddlewareRoutingType.Explicit))
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        return new GraphQLEndpointConventionBuilder(
            endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL Schema Pipeline"));
    }

    /// <summary>
    /// Adds a GraphQL semantic non-null schema SDL endpoint to the endpoint configurations.
    /// The endpoint serves a schema document where non-null wrappers have been replaced with
    /// the @semanticNonNull directive.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL semantic non-null schema endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static IEndpointConventionBuilder MapGraphQLSemanticNonNullSchema(
        this IEndpointRouteBuilder endpointRouteBuilder,
        [StringSyntax("Route")] string pattern = GraphQLSemanticNonNullSchemaPath,
        string? schemaName = null)
        => MapGraphQLSemanticNonNullSchema(endpointRouteBuilder, Parse(pattern), schemaName);

    /// <summary>
    /// Adds a GraphQL semantic non-null schema SDL endpoint to the endpoint configurations.
    /// The endpoint serves a schema document where non-null wrappers have been replaced with
    /// the @semanticNonNull directive.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL semantic non-null schema endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static IEndpointConventionBuilder MapGraphQLSemanticNonNullSchema(
        this IEndpointRouteBuilder endpointRouteBuilder,
        RoutePattern pattern,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);
        ArgumentNullException.ThrowIfNull(pattern);

        TryResolveSchemaName(endpointRouteBuilder.ServiceProvider, ref schemaName);

        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
        var schemaNameOrDefault = schemaName ?? ISchemaDefinition.DefaultName;

        var services = endpointRouteBuilder.ServiceProvider;
        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = services.GetRequiredService<IRequestExecutorEvents>();
        var executor = new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaNameOrDefault);
        var serverOptions = services.GetServerOptions(schemaNameOrDefault);

        requestPipeline
            .Use(MiddlewareFactory.CreateCancellationMiddleware())
            .Use(MiddlewareFactory.CreateHttpGetSemanticNonNullSchemaMiddleware(executor, serverOptions))
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        return new GraphQLEndpointConventionBuilder(
            endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL Semantic Non-Null Schema Pipeline"));
    }

    /// <summary>
    /// Adds a Nitro endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="toolPath">
    /// The path to which Nitro is mapped.
    /// </param>
    /// <param name="relativeRequestPath">
    /// The relative path on which the server is listening for GraphQL requests.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static NitroAppEndpointConventionBuilder MapNitroApp(
        this IEndpointRouteBuilder endpointRouteBuilder,
        string toolPath = GraphQLToolPath,
        string? relativeRequestPath = GraphQLToolRelativeRequestPath)
        => MapNitroApp(endpointRouteBuilder, new PathString(toolPath), relativeRequestPath);

    /// <summary>
    /// Adds a Nitro endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="toolPath">
    /// The path to which Nitro is mapped.
    /// </param>
    /// <param name="relativeRequestPath">
    /// The relative path on which the server is listening for GraphQL requests.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static NitroAppEndpointConventionBuilder MapNitroApp(
        this IEndpointRouteBuilder endpointRouteBuilder,
        PathString toolPath,
        string? relativeRequestPath = GraphQLToolRelativeRequestPath)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);

        toolPath = toolPath.ToString().TrimEnd('/');
        relativeRequestPath ??= GraphQLToolRelativeRequestPath;

        var pattern = Parse(toolPath + "/{**slug}");
        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
        var nitroOptions = new NitroAppOptions { GraphQLEndpoint = relativeRequestPath };

        requestPipeline
            .UseNitroApp(toolPath, nitroOptions)
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        var builder = endpointRouteBuilder
            .Map(pattern, requestPipeline.Build())
            .WithDisplayName("Nitro Pipeline")
            .WithMetadata(nitroOptions);

        return new NitroAppEndpointConventionBuilder(builder);
    }

    /// <summary>
    /// Adds a persisted operation endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointRouteBuilder"/>.
    /// </param>
    /// <param name="path">
    /// The path to which the persisted operation endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <param name="requireOperationName">
    /// Specifies if its required providing the operation name as part of the URI.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// </returns>
    public static IEndpointConventionBuilder MapGraphQLPersistedOperations(
        this IEndpointRouteBuilder endpointRouteBuilder,
        [StringSyntax("Route")] string path = GraphQLPersistedOperationPath,
        string? schemaName = null,
        bool requireOperationName = false)
        => MapGraphQLPersistedOperations(endpointRouteBuilder, Parse(path), schemaName, requireOperationName);

    /// <summary>
    /// Adds a persisted operation endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointRouteBuilder"/>.
    /// </param>
    /// <param name="path">
    /// The path to which the persisted operation endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <param name="requireOperationName">
    /// Specifies if its required providing the operation name as part of the URI.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// </returns>
    public static IEndpointConventionBuilder MapGraphQLPersistedOperations(
        this IEndpointRouteBuilder endpointRouteBuilder,
        RoutePattern path,
        string? schemaName = null,
        bool requireOperationName = false)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);
        ArgumentNullException.ThrowIfNull(path);

        TryResolveSchemaName(endpointRouteBuilder.ServiceProvider, ref schemaName);

        schemaName ??= ISchemaDefinition.DefaultName;
        var group = endpointRouteBuilder.MapGroup(path);
        group.MapPersistedOperationMiddleware(endpointRouteBuilder.ServiceProvider, schemaName, requireOperationName);
        return group;
    }

    /// <summary>
    /// Specifies per-endpoint overrides for <see cref="GraphQLServerOptions"/>.
    /// The overrides are applied on top of the schema-level defaults configured
    /// via <c>ModifyServerOptions</c>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="GraphQLEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the server options for this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="GraphQLEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static GraphQLEndpointConventionBuilder WithOptions(
        this GraphQLEndpointConventionBuilder builder,
        Action<GraphQLServerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Add(c =>
        {
            c.Metadata.Add(new GraphQLServerOptionsOverride(configure));

            var options = new GraphQLServerOptions();

            for (var i = c.Metadata.Count - 1; i >= 0; i--)
            {
                if (c.Metadata[i] is NitroAppOptions toolOptions)
                {
                    options.Tool = toolOptions.Clone();
                    break;
                }
            }

            configure(options);
            c.Metadata.Add(options.Tool);
        });

        return builder;
    }

    /// <summary>
    /// Specifies per-endpoint overrides for <see cref="GraphQLServerOptions"/>.
    /// The overrides are applied on top of the schema-level defaults configured
    /// via <c>ModifyServerOptions</c>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="GraphQLHttpEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the server options for this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="GraphQLHttpEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static GraphQLHttpEndpointConventionBuilder WithOptions(
        this GraphQLHttpEndpointConventionBuilder builder,
        Action<GraphQLServerOptions> configure)
    {
        builder.Add(c => c.Metadata.Add(new GraphQLServerOptionsOverride(configure)));
        return builder;
    }

    /// <summary>
    /// Specifies per-endpoint overrides for <see cref="GraphQLSocketOptions"/>.
    /// The overrides are applied on top of the schema-level defaults configured
    /// via <c>ModifyServerOptions</c>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="GraphQLWebSocketEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the socket options for this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="GraphQLWebSocketEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static GraphQLWebSocketEndpointConventionBuilder WithOptions(
        this GraphQLWebSocketEndpointConventionBuilder builder,
        Action<GraphQLSocketOptions> configure)
    {
        builder.Add(c => c.Metadata.Add(
            new GraphQLServerOptionsOverride(o => configure(o.Sockets))));
        return builder;
    }

    /// <summary>
    /// Specifies the Nitro tool options for this endpoint.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="GraphQLEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the tool options.
    /// </param>
    /// <returns>
    /// Returns the <see cref="GraphQLEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static GraphQLEndpointConventionBuilder WithOptions(
        this GraphQLEndpointConventionBuilder builder,
        Action<NitroAppOptions> configure)
    {
        var options = new NitroAppOptions();
        configure(options);
        builder.Add(c => c.Metadata.Add(options));
        return builder;
    }

    /// <summary>
    /// Specifies the Nitro tool options for this endpoint.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="NitroAppEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the tool options.
    /// </param>
    /// <returns>
    /// Returns the <see cref="NitroAppEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static NitroAppEndpointConventionBuilder WithOptions(
        this NitroAppEndpointConventionBuilder builder,
        Action<NitroAppOptions> configure)
    {
        var options = new NitroAppOptions();
        configure(options);
        builder.Add(c => c.Metadata.Add(options));
        return builder;
    }

    private static GraphQLServerOptions GetServerOptions(this IServiceProvider services, string schemaName)
        => services.GetRequiredService<IOptionsMonitor<GraphQLServerOptions>>().Get(schemaName);

    private static void TryResolveSchemaName(IServiceProvider services, ref string? schemaName)
    {
        if (schemaName is null
            && services.GetService<IRequestExecutorProvider>() is { } provider
            && provider.SchemaNames.Length == 1)
        {
            schemaName = provider.SchemaNames[0];
        }
    }
}
