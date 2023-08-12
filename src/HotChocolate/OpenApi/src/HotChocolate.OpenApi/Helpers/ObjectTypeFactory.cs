using HotChocolate.Skimmed;
using HotChocolate.Types;
using Microsoft.OpenApi.Models;
using INamedType = HotChocolate.Skimmed.INamedType;
using ObjectType = HotChocolate.Skimmed.ObjectType;

namespace HotChocolate.OpenApi.Helpers;

internal static class ObjectTypeFactory
{
    public static INamedType CreateType(OpenApiWrapperContext context, string typeName, OpenApiSchema schema)
    {
        if (context.SkimmedSchema.Types.ContainsName(typeName))
        {
            return context.SkimmedSchema.Types[typeName];
        }

        if (Scalars.IsBuiltIn(typeName))
        {
            return new ObjectType(typeName);
        }

        var type = new ObjectType(typeName)
        {
            Description = schema.Description
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

        if (!context.SkimmedSchema.Types.ContainsName(typeName))
        {
            context.SkimmedSchema.Types.Add(type);
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
            Description = property.Value.Description
        };

        CreateType(context, fieldType.NamedType().Name, typeInfo.RootSchema);
        return field;
    }
}
