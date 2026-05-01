using System.Diagnostics.CodeAnalysis;
using HotChocolate.Adapters.Mcp.Configuration;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Extensions;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode(
    "JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode(
    "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public static class FusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder AddMcp(
        this IFusionGatewayBuilder builder,
        Action<McpServerOptions>? configureServerOptions = null,
        Action<IMcpServerBuilder>? configureServer = null,
        Func<IServiceProvider, bool>? skipIf = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddMcpServices();
        builder.Services.ConfigureMcpSetup(builder.Name, configureServerOptions, configureServer);

        builder.ConfigureSchemaServices(
            (applicationServices, schemaServices) =>
                schemaServices.AddMcpSchemaServices(applicationServices, builder.Name));

        builder.AddWarmupTask(
            schemaServices => new McpStorageWarmupTask(schemaServices.GetRequiredService<McpStorageObserver>()),
            skipIf);

        return builder;
    }

    /// <summary>
    /// Adds an MCP storage to the gateway.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="storage">
    /// The MCP storage instance.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="storage"/> is <c>null</c>.
    /// </exception>
    public static IFusionGatewayBuilder AddMcpStorage(
        this IFusionGatewayBuilder builder,
        IMcpStorage storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        builder.Services.Configure<McpSetup>(
            builder.Name,
            setup => setup.StorageFactory = _ => storage);

        return builder;
    }

    /// <summary>
    /// Adds an MCP storage to the gateway.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The type of the MCP storage.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the application services.
    /// </remarks>
    public static IFusionGatewayBuilder AddMcpStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IMcpStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<McpSetup>(
            builder.Name,
            setup => setup.StorageFactory = ActivatorUtilities.GetServiceOrCreateInstance<T>);

        return builder;
    }

    /// <summary>
    /// Adds an MCP storage to the gateway.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// The factory to create the MCP storage.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="factory"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the application services.
    /// </remarks>
    public static IFusionGatewayBuilder AddMcpStorage(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, IMcpStorage> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.Services.Configure<McpSetup>(
            builder.Name,
            setup => setup.StorageFactory = factory);

        return builder;
    }
}
