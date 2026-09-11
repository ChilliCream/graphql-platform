using System.Diagnostics.CodeAnalysis;
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Fusion.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

// The legacy extension surface holds the shared operations until the compatibility surface
// is removed. Calling it here forwards through the same configuration pipeline.
#pragma warning disable CS0618

/// <summary>
/// Provides extension methods for <see cref="IFusionRouterBuilder"/> to configure OpenAPI support.
/// </summary>
public static class OpenApiFusionRouterBuilderExtensions
{
    /// <summary>
    /// Registers the OpenAPI integration services for the router.
    /// </summary>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="skipIf"/>
    /// is for the application services.
    /// </remarks>
    public static IFusionRouterBuilder AddOpenApi(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, bool>? skipIf = null)
    {
        OpenApiFusionGatewayBuilderExtensions.AddOpenApi(builder, skipIf);
        return builder;
    }

    /// <summary>
    /// Adds an OpenAPI definition storage to the router.
    /// </summary>
    public static IFusionRouterBuilder AddOpenApiDefinitionStorage(
        this IFusionRouterBuilder builder,
        IOpenApiDefinitionStorage storage)
    {
        OpenApiFusionGatewayBuilderExtensions.AddOpenApiDefinitionStorage(builder, storage);
        return builder;
    }

    /// <summary>
    /// Adds an OpenAPI definition storage to the router.
    /// </summary>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the application services.
    /// </remarks>
    public static IFusionRouterBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder)
        where T : class, IOpenApiDefinitionStorage
    {
        OpenApiFusionGatewayBuilderExtensions.AddOpenApiDefinitionStorage<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds an OpenAPI definition storage to the router.
    /// </summary>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the application services.
    /// </remarks>
    public static IFusionRouterBuilder AddOpenApiDefinitionStorage(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, IOpenApiDefinitionStorage> factory)
    {
        OpenApiFusionGatewayBuilderExtensions.AddOpenApiDefinitionStorage(builder, factory);
        return builder;
    }
}
