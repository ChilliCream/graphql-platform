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
        requestPipeline.MapGraphQL(path, schemaNameOrDefault);

        return new GraphQLEndpointConventionBuilder(
            endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL Pipeline"));
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

        var executorProvider = applicationBuilder.ApplicationServices.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = applicationBuilder.ApplicationServices.GetRequiredService<IRequestExecutorEvents>();
        var formOptions = applicationBuilder.ApplicationServices.GetRequiredService<IOptions<FormOptions>>();
        var executor = new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaName);

        applicationBuilder
            .Use(MiddlewareFactory.CreateCancellationMiddleware())
            .Use(MiddlewareFactory.CreateWebSocketSubscriptionMiddleware(executor))
            .Use(MiddlewareFactory.CreateHttpPostMiddleware(executor))
            .Use(MiddlewareFactory.CreateHttpMultipartMiddleware(executor, formOptions))
            .Use(MiddlewareFactory.CreateHttpGetMiddleware(executor))
            .Use(MiddlewareFactory.CreateHttpGetSchemaMiddleware(executor, path, MiddlewareRoutingType.Integrated))
            .UseNitroApp(path)
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

        var executorProvider = endpointRouteBuilder.ServiceProvider.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = endpointRouteBuilder.ServiceProvider.GetRequiredService<IRequestExecutorEvents>();
        var formOptions = endpointRouteBuilder.ServiceProvider.GetRequiredService<IOptions<FormOptions>>();
        var executor = new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaNameOrDefault);

        requestPipeline
            .Use(MiddlewareFactory.CreateCancellationMiddleware())
            .Use(MiddlewareFactory.CreateHttpPostMiddleware(executor))
            .Use(MiddlewareFactory.CreateHttpMultipartMiddleware(executor, formOptions))
            .Use(MiddlewareFactory.CreateHttpGetMiddleware(executor))
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
    public static WebSocketEndpointConventionBuilder MapGraphQLWebSocket(
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
    public static WebSocketEndpointConventionBuilder MapGraphQLWebSocket(
        this IEndpointRouteBuilder endpointRouteBuilder,
        RoutePattern pattern,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);
        ArgumentNullException.ThrowIfNull(pattern);

        TryResolveSchemaName(endpointRouteBuilder.ServiceProvider, ref schemaName);

        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
        var schemaNameOrDefault = schemaName ?? ISchemaDefinition.DefaultName;

        var executorProvider = endpointRouteBuilder.ServiceProvider.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = endpointRouteBuilder.ServiceProvider.GetRequiredService<IRequestExecutorEvents>();
        var executor = new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaNameOrDefault);

        requestPipeline
            .Use(MiddlewareFactory.CreateCancellationMiddleware())
            .Use(MiddlewareFactory.CreateWebSocketSubscriptionMiddleware(executor))
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        var builder = new GraphQLEndpointConventionBuilder(
            endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL WebSocket Pipeline"));

        return new WebSocketEndpointConventionBuilder(builder);
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

        var executorProvider = endpointRouteBuilder.ServiceProvider.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = endpointRouteBuilder.ServiceProvider.GetRequiredService<IRequestExecutorEvents>();
        var executor = new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaNameOrDefault);

        requestPipeline
            .Use(MiddlewareFactory.CreateCancellationMiddleware())
            .Use(MiddlewareFactory.CreateHttpGetSchemaMiddleware(executor, PathString.Empty, MiddlewareRoutingType.Explicit))
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

        requestPipeline
            .UseNitroApp(toolPath)
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        var builder = endpointRouteBuilder
            .Map(pattern, requestPipeline.Build())
            .WithDisplayName("Nitro Pipeline")
            .WithMetadata(new NitroAppOptions { GraphQLEndpoint = relativeRequestPath });

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
    /// Specifies the GraphQL server options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="GraphQLEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="serverOptions">
    /// The GraphQL server options.
    /// </param>
    /// <returns>
    /// Returns the <see cref="GraphQLEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static GraphQLEndpointConventionBuilder WithOptions(
        this GraphQLEndpointConventionBuilder builder,
        GraphQLServerOptions serverOptions)
        => builder
            .WithMetadata(serverOptions)
            .WithMetadata(serverOptions.Tool.ToNitroAppOptions());

    /// <summary>
    /// Specifies the GraphQL HTTP request options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="GraphQLEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="httpOptions">
    /// The GraphQL HTTP request options.
    /// </param>
    /// <returns>
    /// Returns the <see cref="GraphQLEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static GraphQLHttpEndpointConventionBuilder WithOptions(
        this GraphQLHttpEndpointConventionBuilder builder,
        GraphQLHttpOptions httpOptions) =>
        builder.WithMetadata(
            new GraphQLServerOptions
            {
                AllowedGetOperations = httpOptions.AllowedGetOperations,
                EnableGetRequests = httpOptions.EnableGetRequests,
                EnableMultipartRequests = httpOptions.EnableMultipartRequests
            });

    /// <summary>
    /// Specifies the Nitro tooling options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="NitroAppEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="toolOptions">
    /// The Nitro tooling options.
    /// </param>
    /// <returns>
    /// Returns the <see cref="NitroAppEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static NitroAppEndpointConventionBuilder WithOptions(
        this NitroAppEndpointConventionBuilder builder,
        GraphQLToolOptions toolOptions)
    {
        builder.Add(c => c.Metadata.Add(toolOptions.ToNitroAppOptions()));
        return builder;
    }

    /// <summary>
    /// Specifies the GraphQL over Websocket options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebSocketEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="socketOptions">
    /// The GraphQL socket options.
    /// </param>
    /// <returns>
    /// Returns the <see cref="WebSocketEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static WebSocketEndpointConventionBuilder WithOptions(
        this WebSocketEndpointConventionBuilder builder,
        GraphQLSocketOptions socketOptions) =>
        builder.WithMetadata(new GraphQLServerOptions { Sockets = socketOptions });

    private static void TryResolveSchemaName(IServiceProvider services, ref string? schemaName)
    {
        if (schemaName is null
            && services.GetService<IRequestExecutorProvider>() is { } provider
            && provider.SchemaNames.Length == 1)
        {
            schemaName = provider.SchemaNames[0];
        }
    }

    internal static NitroAppOptions ToNitroAppOptions(this GraphQLToolOptions options)
        => new()
        {
            ServeMode = ServeMode.Version(options.ServeMode.Mode),
            Title = options.Title,
            Document = options.Document,
            UseBrowserUrlAsGraphQLEndpoint = options.UseBrowserUrlAsGraphQLEndpoint,
            GraphQLEndpoint = options.GraphQLEndpoint,
            IncludeCookies = options.IncludeCookies,
            HttpHeaders = options.HttpHeaders,
            UseGet = options.HttpMethod == DefaultHttpMethod.Get,
            Enable = options.Enable,
            GaTrackingId = options.GaTrackingId,
            DisableTelemetry = options.DisableTelemetry
        };
}
