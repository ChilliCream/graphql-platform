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

        builder.Services.AddKeyedSingleton(builder.Name, storage);

        return builder;
    }

    public static IRequestExecutorBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenApiDefinitionStorageCore();

        builder.Services.AddKeyedSingleton<IOpenApiDefinitionStorage, T>(builder.Name);

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

        builder.Services.AddKeyedSingleton<IOpenApiDefinitionStorage, T>(
            builder.Name,
            (sp, _) => factory(sp));

        return builder;
    }

    private static void AddOpenApiDefinitionStorageCore(this IRequestExecutorBuilder builder)
    {
        var schemaName = builder.Name;

        builder.Services.AddOpenApiServices(schemaName);
        builder.Services.AddOpenApiAspNetCoreServices(schemaName);

        builder.ConfigureSchemaServices((_, schemaServices) =>
        {
            schemaServices.TryAddSingleton<IOpenApiResultFormatter, OpenApiResultFormatter>();
            schemaServices.AddOpenApiSchemaServices();
        });

        builder.AddWarmupTask(schemaServices =>
        {
            var registry = schemaServices.GetRootServiceProvider()
                .GetRequiredKeyedService<OpenApiDefinitionRegistry>(schemaName);

            return new OpenApiWarmupTask(registry);
        });
    }
}
