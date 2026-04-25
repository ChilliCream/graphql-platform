using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

public static class OpenApiOptionsExtensions
{
    public static OpenApiOptions AddGraphQLTransformer(this OpenApiOptions options, string? schemaName = null)
    {
        return options.AddDocumentTransformer((document, context, ct) =>
        {
            var manager = context.ApplicationServices.GetRequiredService<OpenApiManager>();

            var resolvedSchemaName = schemaName;
            TryResolveSchemaName(manager, ref resolvedSchemaName);
            resolvedSchemaName ??= ISchemaDefinition.DefaultName;

            var transformer = (DynamicOpenApiDocumentTransformer)manager
                .Get(resolvedSchemaName)
                .DocumentTransformer;

            return transformer.TransformAsync(document, context, ct);
        });
    }

    private static void TryResolveSchemaName(IOpenApiProvider provider, ref string? schemaName)
    {
        if (schemaName is null && provider.Names.Length == 1)
        {
            schemaName = provider.Names[0];
        }
    }
}
