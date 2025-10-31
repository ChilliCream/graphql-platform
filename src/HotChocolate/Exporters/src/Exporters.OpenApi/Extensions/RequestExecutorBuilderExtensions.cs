using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Exporters.OpenApi;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddOpenApiDefinitionStorage(
        this IRequestExecutorBuilder builder,
        IOpenApiDefinitionStorage definitionStorage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(definitionStorage);

        builder.AddOpenApiDocumentStorageCore();

        return builder.ConfigureSchemaServices(s => s.AddSingleton(definitionStorage));
    }

    public static IRequestExecutorBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenApiDocumentStorageCore();

        return builder.ConfigureSchemaServices(
            static s => s.AddSingleton<IOpenApiDefinitionStorage, T>());
    }

    private static void AddOpenApiDocumentStorageCore(this IRequestExecutorBuilder builder)
    {
        var schemaName = builder.Name;

        builder.Services.AddSingleton<IOpenApiResultFormatter, OpenApiResultFormatter>();
        builder.Services.AddOpenApiExporterServices(schemaName);

        builder.ConfigureSchemaServices((applicationServices, services)
            => services.AddOpenApiExporterSchemaServices(schemaName, applicationServices));

        builder.AddWarmupTask<OpenApiWarmupTask>();
    }
}
