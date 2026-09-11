using System.Diagnostics.CodeAnalysis;
using HotChocolate.Adapters.Mcp.Configuration;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Fusion.Configuration;
using ModelContextProtocol.Server;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

// The legacy extension surface holds the shared operations until the compatibility surface
// is removed. Calling it here forwards through the same configuration pipeline.
#pragma warning disable CS0618

/// <summary>
/// Provides extension methods for <see cref="IFusionRouterBuilder"/> to configure MCP tool support.
/// </summary>
#if !NET9_0_OR_GREATER
[RequiresDynamicCode(
    "JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode(
    "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public static class FusionRouterBuilderExtensions
{
    /// <summary>
    /// Adds MCP tool support to the router.
    /// </summary>
    public static IFusionRouterBuilder AddMcp(
        this IFusionRouterBuilder builder,
        Action<McpServerOptions>? configureServerOptions = null,
        Action<IMcpServerBuilder>? configureServer = null,
        Func<IServiceProvider, bool>? skipIf = null)
    {
        FusionGatewayBuilderExtensions.AddMcp(builder, configureServerOptions, configureServer, skipIf);
        return builder;
    }

    /// <summary>
    /// Modifies the options that control how MCP tools are generated from operations.
    /// </summary>
    public static IFusionRouterBuilder ModifyMcpToolOptions(
        this IFusionRouterBuilder builder,
        Action<McpToolOptions> configure)
    {
        FusionGatewayBuilderExtensions.ModifyMcpToolOptions(builder, configure);
        return builder;
    }

    /// <summary>
    /// Adds an MCP storage to the router.
    /// </summary>
    public static IFusionRouterBuilder AddMcpStorage(
        this IFusionRouterBuilder builder,
        IMcpStorage storage)
    {
        FusionGatewayBuilderExtensions.AddMcpStorage(builder, storage);
        return builder;
    }

    /// <summary>
    /// Adds an MCP storage to the router.
    /// </summary>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the application services.
    /// </remarks>
    public static IFusionRouterBuilder AddMcpStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder)
        where T : class, IMcpStorage
    {
        FusionGatewayBuilderExtensions.AddMcpStorage<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds an MCP storage to the router.
    /// </summary>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the application services.
    /// </remarks>
    public static IFusionRouterBuilder AddMcpStorage(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, IMcpStorage> factory)
    {
        FusionGatewayBuilderExtensions.AddMcpStorage(builder, factory);
        return builder;
    }
}
