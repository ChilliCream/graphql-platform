using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Configuration;
using HotChocolate.Exporters.OpenApi;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

// TODO: Also add Fusion variants
public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddOpenApiDocumentStorage(
        this IRequestExecutorBuilder builder,
        IOpenApiDocumentStorage documentStorage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(documentStorage);

        builder.Services.TryAddKeyedSingleton<DynamicEndpointDataSource>(builder.Name);

        return builder.ConfigureSchemaServices(s => s.AddSingleton(documentStorage));
    }

    public static IRequestExecutorBuilder AddOpenApiDocumentStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder)
        where T : class, IOpenApiDocumentStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchemaServices(static s => s.AddSingleton<IOpenApiDocumentStorage, T>());
    }
}
