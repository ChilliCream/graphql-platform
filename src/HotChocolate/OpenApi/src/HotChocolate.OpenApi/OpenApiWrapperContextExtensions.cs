using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;

internal static class OpenApiWrapperContextExtensions
{
    public static OpenApiSchema? GetSchema(this OpenApiWrapperContext ctx, string? name)
    {
        ctx.OpenApiDocument.Components.Schemas.TryGetValue(name ?? string.Empty, out var schema);
        return schema;
    }

    public static List<KeyValuePair<string, Operation>> GetQueryOperations(this OpenApiWrapperContext ctx) 
        => ctx.Operations.Where(o => o.Value.Method == HttpMethod.Get).ToList();

    public static List<KeyValuePair<string, Operation>> GetMutationOperations(this OpenApiWrapperContext ctx) 
        => ctx.Operations.Where(o => o.Value.Method != HttpMethod.Get).ToList();
}
