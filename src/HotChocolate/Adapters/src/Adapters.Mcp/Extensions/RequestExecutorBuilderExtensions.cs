using System.Diagnostics.CodeAnalysis;
using HotChocolate.Adapters.Mcp.Configuration;
using HotChocolate.Adapters.Mcp.Directives;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Extensions;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMcp(
        this IRequestExecutorBuilder builder,
        Action<McpServerOptions>? configureServerOptions = null,
        Action<IMcpServerBuilder>? configureServer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddMcpServices();

        builder.Services.Configure<McpSetup>(
            builder.Name,
            setup =>
            {
                if (configureServerOptions is not null)
                {
                    setup.ServerOptionsModifiers.Add(configureServerOptions);
                }

                if (configureServer is not null)
                {
                    setup.ServerModifiers.Add(configureServer);
                }
            });

        builder.ConfigureSchemaServices(
            (applicationServices, schemaServices) =>
                schemaServices.AddMcpSchemaServices(applicationServices, builder.Name));

        builder.AddDirectiveType<McpToolAnnotationsDirectiveType>();

        return builder;
    }

    public static IRequestExecutorBuilder AddMcpStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory,
        Func<IServiceProvider, bool>? skipIf = null)
        where T : class, IMcpStorage
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.ConfigureSchemaServices(s => s.AddSingleton<IMcpStorage, T>(factory));

        return builder.AddMcpStorageWarmupTask(skipIf);
    }

    public static IRequestExecutorBuilder AddMcpStorage(
        this IRequestExecutorBuilder builder,
        IMcpStorage storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        builder.ConfigureSchemaServices(s => s.AddSingleton(storage));

        return builder.AddMcpStorageWarmupTask();
    }

    private static IRequestExecutorBuilder AddMcpStorageWarmupTask(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, bool>? skipIf = null)
    {
        builder.AddWarmupTask(
            factory: schemaServices => new McpStorageWarmupTask(
                schemaServices.GetRequiredService<McpStorageObserver>()),
            skipIf: skipIf);

        return builder;
    }
}
