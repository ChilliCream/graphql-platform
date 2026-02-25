using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

public static class OpenApiOptionsExtensions
{
    public static OpenApiOptions AddGraphQLTransformer(this OpenApiOptions options, string? schemaName = null)
    {
        schemaName ??= ISchemaDefinition.DefaultName;

        return options.AddDocumentTransformer((document, context, ct) =>
        {
            var transformer = context.ApplicationServices
                .GetRequiredKeyedService<DynamicOpenApiDocumentTransformer>(schemaName);

            return transformer.TransformAsync(document, context, ct);
        });
    }
}
