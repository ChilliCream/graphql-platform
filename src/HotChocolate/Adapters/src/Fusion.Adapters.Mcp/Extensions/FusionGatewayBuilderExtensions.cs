using System.Diagnostics.CodeAnalysis;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Extensions;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public static class FusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder AddMcp(
        this IFusionGatewayBuilder builder,
        Action<McpServerOptions>? configureServerOptions = null,
        Action<IMcpServerBuilder>? configureServer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddMcpServices();
        builder.Services.ConfigureMcpSetup(builder.Name, configureServerOptions, configureServer);

        builder.ConfigureSchemaServices(
            (applicationServices, schemaServices) =>
                schemaServices.AddMcpSchemaServices(applicationServices, builder.Name));

        return builder;
    }

    public static IFusionGatewayBuilder AddMcpStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory,
        Func<IServiceProvider, bool>? skipIf = null)
        where T : class, IMcpStorage
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.ConfigureSchemaServices((_, s) => s.AddSingleton<IMcpStorage, T>(factory));

        return builder.AddMcpStorageWarmupTask(skipIf);
    }

    public static IFusionGatewayBuilder AddMcpStorage(
        this IFusionGatewayBuilder builder,
        IMcpStorage storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        builder.ConfigureSchemaServices((_, s) => s.AddSingleton(storage));

        return builder.AddMcpStorageWarmupTask();
    }

    private static IFusionGatewayBuilder AddMcpStorageWarmupTask(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, bool>? skipIf = null)
    {
        builder.AddWarmupTask(
            factory: schemaServices => new McpStorageWarmupTask(
                schemaServices.GetRequiredService<McpStorageObserver>()),
            skipIf: skipIf);

        return builder;
    }
}
