#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Adapters.OpenApi;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal static class InternalServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiServices(
        this IServiceCollection applicationServices,
        string schemaName)
    {
        applicationServices.TryAddKeyedSingleton(
            schemaName,
            static (sp, name) => new OpenApiDefinitionRegistry(
                sp.GetRequiredKeyedService<IOpenApiDefinitionStorage>(name),
                sp.GetRequiredKeyedService<IDynamicOpenApiDocumentTransformer>(name),
                sp.GetRequiredKeyedService<IDynamicEndpointDataSource>(name)));

        applicationServices.TryAddKeyedSingleton(
            schemaName,
            static (sp, name) => new HttpRequestExecutorProxy(
                sp.GetRequiredService<IRequestExecutorProvider>(),
                sp.GetRequiredService<IRequestExecutorEvents>(),
                (string)name));

        return applicationServices;
    }

    public static IServiceCollection AddOpenApiSchemaServices(
        this IServiceCollection schemaServices)
    {
        schemaServices.TryAddSingleton<IOpenApiDiagnosticEvents>(sp =>
        {
            var listeners = sp.GetServices<IOpenApiDiagnosticEventListener>().ToArray();
            return listeners.Length switch
            {
                0 => new NoOpOpenApiDiagnosticEventListener(),
                1 => listeners[0],
                _ => new AggregateOpenApiDiagnosticEventListener(listeners)
            };
        });

        return schemaServices;
    }
}
