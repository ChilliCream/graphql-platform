using System.Text.Json;
using HotChocolate.Resolvers;

namespace HotChocolate.OpenApi.FieldMiddleware;

internal sealed class OpenApiFieldMiddleware(FieldDelegate next)
{
    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        var parent = context.Parent<JsonElement?>();
        var field = context.Selection.Field;

        if (field.ContextData.TryGetValue(
                WellKnownContextData.OpenApiResolver, out var resolverValue) &&
            resolverValue is Func<IResolverContext, Task<JsonElement>> resolver)
        {
            context.Result = await resolver.Invoke(context);
        }
        else if (field.ContextData.TryGetValue(
                WellKnownContextData.OpenApiPropertyName, out var propertyNameValue) &&
            propertyNameValue is string propertyName &&
            parent?.TryGetProperty(propertyName, out var propertyValue) == true)
        {
            context.Result = propertyValue.ValueKind is JsonValueKind.Null
                ? null
                : propertyValue.Deserialize(field.RuntimeType);
        }
        else if (field.ContextData.ContainsKey(WellKnownContextData.OpenApiUseParentResult))
        {
            context.Result = parent;
        }
        else if (field.ContextData.ContainsKey(WellKnownContextData.OpenApiIsErrorsField))
        {
            context.Result = (List<JsonElement?>)[parent];
        }

        await next(context);
    }
}
