using System.Diagnostics.CodeAnalysis;
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Adapters.OpenApi.Configuration;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class OpenApiFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Registers the OpenAPI integration services for the Fusion gateway.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="skipIf">
    /// A function that is called to determine if the warmup task should be registered or not.
    /// If <c>true</c> is returned, the warmup task will not be registered.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="skipIf"/>
    /// is for the application services.
    /// </remarks>
    public static IFusionGatewayBuilder AddOpenApi(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, bool>? skipIf = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddOpenApiServices();
        builder.Services.AddOpenApiAspNetCoreServices();

        builder.ConfigureSchemaServices((_, schemaServices) =>
        {
            schemaServices.TryAddSingleton<IOpenApiResultFormatter, FusionOpenApiResultFormatter>();
            schemaServices.AddOpenApiSchemaServices();
        });

        var schemaName = builder.Name;

        builder.AddWarmupTask(
            factory: schemaServices =>
            {
                var registry = schemaServices.GetRootServiceProvider()
                    .GetRequiredService<OpenApiManager>()
                    .Get(schemaName)
                    .Registry;

                return new OpenApiWarmupTask(registry);
            },
            skipIf: skipIf);

        return builder;
    }

    /// <summary>
    /// Adds an OpenAPI definition storage to the gateway.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="storage">
    /// The OpenAPI definition storage instance.
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
    public static IFusionGatewayBuilder AddOpenApiDefinitionStorage(
        this IFusionGatewayBuilder builder,
        IOpenApiDefinitionStorage storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        builder.Services.Configure<OpenApiSetup>(
            builder.Name,
            setup => setup.StorageFactory = _ => storage);

        return builder;
    }

    /// <summary>
    /// Adds an OpenAPI definition storage to the gateway.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
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
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the application services.
    /// </remarks>
    public static IFusionGatewayBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<OpenApiSetup>(
            builder.Name,
            setup => setup.StorageFactory = ActivatorUtilities.GetServiceOrCreateInstance<T>);

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
    public static IFusionGatewayBuilder AddOpenApiDefinitionStorage(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, IOpenApiDefinitionStorage> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.Services.Configure<OpenApiSetup>(
            builder.Name,
            setup => setup.StorageFactory = factory);

        return builder;
    }
}
