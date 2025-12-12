using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Configuration;
using HotChocolate.Adapters.OpenApi;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddOpenApiDocumentStorage(
        this IRequestExecutorBuilder builder,
        IOpenApiDocumentStorage storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        builder.AddOpenApiDocumentStorageCore();

        builder.Services.AddKeyedSingleton(builder.Name, storage);

        return builder;
    }

    public static IRequestExecutorBuilder AddOpenApiDocumentStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder)
        where T : class, IOpenApiDocumentStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenApiDocumentStorageCore();

        builder.Services.AddKeyedSingleton<IOpenApiDocumentStorage, T>(builder.Name);

        return builder;
    }

    private static void AddOpenApiDocumentStorageCore(this IRequestExecutorBuilder builder)
    {
        var schemaName = builder.Name;

        builder.Services.AddOpenApiExporterServices(schemaName);
        builder.Services.AddOpenApiAspNetCoreServices(schemaName);

        builder.ConfigureSchemaServices((applicationServices, services) =>
        {
            services.TryAddSingleton<IOpenApiResultFormatter, OpenApiResultFormatter>();
            services.AddOpenApiExporterSchemaServices(schemaName, applicationServices);
        });

        builder.AddWarmupTask<OpenApiWarmupTask>();
    }
}
