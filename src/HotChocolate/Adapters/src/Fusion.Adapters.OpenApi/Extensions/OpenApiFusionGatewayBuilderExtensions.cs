using System.Diagnostics.CodeAnalysis;
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class OpenApiFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds an OpenAPI definition storage to the gateway.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="storage">
    /// The OpenAPI definition storage instance.
    /// </param>
    /// <param name="skipIf">
    /// A function that is called to determine if the storage should be registered or not.
    /// If <c>true</c> is returned, the storage will not be registered.
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
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="skipIf"/>
    /// is for the application services.
    /// </remarks>
    public static IFusionGatewayBuilder AddOpenApiDefinitionStorage(
        this IFusionGatewayBuilder builder,
        IOpenApiDefinitionStorage storage,
        Func<IServiceProvider, bool>? skipIf = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        builder.AddOpenApiDefinitionStorageCore(skipIf);

        builder.Services.AddKeyedSingleton(builder.Name, storage);

        return builder;
    }

    /// <summary>
    /// Adds an OpenAPI definition storage to the gateway.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="skipIf">
    /// A function that is called to determine if the storage should be registered or not.
    /// If <c>true</c> is returned, the storage will not be registered.
    /// </param>
    /// <typeparam name="T">
    /// The type of the OpenAPI definition storage.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the schema services.
    /// If your <typeparamref name="T"/> needs to access application services you need to
    /// make the services available in the schema services via <see cref="CoreFusionGatewayBuilderExtensions.AddApplicationService"/>.
    /// <br />
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="skipIf"/>
    /// is for the application services.
    /// </remarks>
    public static IFusionGatewayBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, bool>? skipIf = null)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenApiDefinitionStorageCore(skipIf);

        builder.Services.AddKeyedSingleton<IOpenApiDefinitionStorage, T>(builder.Name);

        return builder;
    }

    /// <summary>
    /// Adds an OpenAPI definition storage to the gateway.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// The factory to create the OpenAPI definition storage.
    /// </param>
    /// <param name="skipIf">
    /// A function that is called to determine if the storage should be registered or not.
    /// If <c>true</c> is returned, the storage will not be registered.
    /// </param>
    /// <typeparam name="T">
    /// The type of the OpenAPI definition storage.
    /// </typeparam>
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
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="CoreFusionGatewayBuilderExtensions.AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// <br />
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="skipIf"/>
    /// is for the application services.
    /// </remarks>
    public static IFusionGatewayBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory,
        Func<IServiceProvider, bool>? skipIf = null)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.AddOpenApiDefinitionStorageCore(skipIf);

        builder.Services.AddKeyedSingleton<IOpenApiDefinitionStorage, T>(
            builder.Name,
            (sp, _) => factory(sp));

        return builder;
    }

    private static void AddOpenApiDefinitionStorageCore(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, bool>? skipIf)
    {
        var schemaName = builder.Name;

        builder.Services.AddOpenApiServices(schemaName);
        builder.Services.AddOpenApiAspNetCoreServices(schemaName);

        builder.ConfigureSchemaServices((_, schemaServices) =>
        {
            schemaServices.TryAddSingleton<IOpenApiResultFormatter, FusionOpenApiResultFormatter>();
            schemaServices.AddOpenApiSchemaServices();
        });

        builder.AddWarmupTask(
            factory: schemaServices =>
            {
                var registry = schemaServices.GetRootServiceProvider()
                    .GetRequiredKeyedService<OpenApiDefinitionRegistry>(schemaName);

                return new OpenApiWarmupTask(registry);
            },
            skipIf: skipIf);
    }
}
