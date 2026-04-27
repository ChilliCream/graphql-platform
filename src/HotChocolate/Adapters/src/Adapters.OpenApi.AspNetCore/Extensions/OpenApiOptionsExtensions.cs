using HotChocolate.Execution;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

public static class OpenApiOptionsExtensions
{
    public static OpenApiOptions AddGraphQLTransformer(this OpenApiOptions options, string? schemaName = null)
    {
        return options.AddDocumentTransformer((document, context, ct) =>
        {
            var resolvedSchemaName = schemaName;
            TryResolveSchemaName(context.ApplicationServices, ref resolvedSchemaName);
            resolvedSchemaName ??= ISchemaDefinition.DefaultName;

            var transformer = context.ApplicationServices
                .GetRequiredKeyedService<DynamicOpenApiDocumentTransformer>(resolvedSchemaName);

            return transformer.TransformAsync(document, context, ct);
        });
    }

    private static void TryResolveSchemaName(IServiceProvider services, ref string? schemaName)
    {
        if (schemaName is null
            && services.GetService<IRequestExecutorProvider>() is { } provider
            && provider.SchemaNames.Length == 1)
        {
            schemaName = provider.SchemaNames[0];
        }
    }
}
