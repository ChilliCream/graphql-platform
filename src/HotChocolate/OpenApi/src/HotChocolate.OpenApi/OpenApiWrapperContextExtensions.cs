using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;

internal static class OpenApiWrapperContextExtensions
{
    public static OpenApiSchema? GetSchema(this OpenApiWrapperContext ctx, string? name)
    {
        ctx.OpenApiDocument.Components.Schemas.TryGetValue(name ?? string.Empty, out var schema);
        return schema;
    }
}
