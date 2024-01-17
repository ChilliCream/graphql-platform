using HotChocolate.OpenApi.Properties;
using HotChocolate.Skimmed;
using HotChocolate.Types;
using Microsoft.OpenApi.Models;
using INamedType = HotChocolate.Skimmed.INamedType;
using ObjectType = HotChocolate.Skimmed.ObjectType;

namespace HotChocolate.OpenApi.Helpers;

internal static class ObjectTypeFactory
{
    /// <summary>
    /// Parses the <paramref name="schema"/> and it fields and adds new object types
    /// to context if necessary
    /// </summary>
    /// <param name="context"></param>
    /// <param name="typeName"></param>
    /// <param name="schema"></param>
    /// <returns></returns>
    public static INamedType ParseType(OpenApiWrapperContext context, string typeName, OpenApiSchema schema)
    {
        if (context.MutableSchema.Types.ContainsName(typeName))
        {
            return context.MutableSchema.Types[typeName];
        }

        if (Scalars.IsBuiltIn(typeName))
        {
            return new ObjectType(typeName);
        }

        var type = new ObjectType(typeName)
        {
            Description = schema.Description,
        };

        var typeInfo = context.GetSchemaTypeInfo(schema);

        foreach (var property in schema.Properties)
        {
            var field = CreateField(context, typeInfo.RootSchema, property);
            type.Fields.Add(field);
        }

        foreach (var allOf in schema.AllOf)
        {
            foreach (var allOfProperty in allOf.Properties)
            {
                var field = CreateField(context, typeInfo.RootSchema, allOfProperty);
                type.Fields.Add(field);
            }
        }

        if (!context.MutableSchema.Types.ContainsName(typeName))
        {
            context.MutableSchema.Types.Add(type);
        }

        return type;
    }

    private static OutputField CreateField(OpenApiWrapperContext context, OpenApiSchema schema, KeyValuePair<string, OpenApiSchema> property)
    {
        var typeInfo = context.GetSchemaTypeInfo(property.Value);
        var isRequired = schema.Required.Contains(property.Key);
        var fieldType = typeInfo.GetGraphQLTypeNode(isRequired);
        var field = new OutputField(OpenApiNamingHelper.GetFieldName(property.Key))
        {
            Type = fieldType,
            Description = property.Value.Description,
            ContextData =
            {
                [OpenApiResources.OpenApiPropertyName] = property.Key,
            },
        };

        ParseType(context, fieldType.NamedType().Name, typeInfo.RootSchema);
        return field;
    }
}
