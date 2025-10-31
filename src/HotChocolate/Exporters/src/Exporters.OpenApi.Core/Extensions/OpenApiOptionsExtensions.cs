using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

public static class OpenApiOptionsExtensions
{
    // TODO: Better name
    public static OpenApiOptions AddGraphQL(this OpenApiOptions options, string? schemaName = null)
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
