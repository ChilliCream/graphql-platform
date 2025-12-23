using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Configuration;
using HotChocolate.Adapters.OpenApi;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddOpenApiDefinitionStorage(
        this IRequestExecutorBuilder builder,
        IOpenApiDefinitionStorage storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        builder.AddOpenApiDefinitionStorageCore();

        builder.ConfigureSchemaServices(services => services.AddSingleton(storage));

        return builder;
    }

    public static IRequestExecutorBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenApiDefinitionStorageCore();

        builder.ConfigureSchemaServices(services => services.AddSingleton<IOpenApiDefinitionStorage, T>());

        return builder;
    }

    public static IRequestExecutorBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.AddOpenApiDefinitionStorageCore();

        builder.ConfigureSchemaServices(services => services.AddSingleton<IOpenApiDefinitionStorage, T>(factory));

        return builder;
    }

    private static void AddOpenApiDefinitionStorageCore(this IRequestExecutorBuilder builder)
    {
        var schemaName = builder.Name;

        builder.Services.AddOpenApiServices(schemaName);
        builder.Services.AddOpenApiAspNetCoreServices(schemaName);

        builder.ConfigureSchemaServices((applicationServices, services) =>
        {
            services.TryAddSingleton<IOpenApiResultFormatter, OpenApiResultFormatter>();
            services.AddOpenApiSchemaServices(schemaName, applicationServices);
        });

        builder.AddWarmupTask<OpenApiWarmupTask>();
    }
}
