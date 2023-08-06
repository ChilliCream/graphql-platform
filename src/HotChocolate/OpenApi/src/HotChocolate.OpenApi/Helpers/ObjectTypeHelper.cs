using HotChocolate.Skimmed;
using HotChocolate.Types;
using Microsoft.OpenApi.Models;
using INamedType = HotChocolate.Skimmed.INamedType;
using ObjectType = HotChocolate.Skimmed.ObjectType;

namespace HotChocolate.OpenApi.Helpers;

internal static class ObjectTypeHelper
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

        foreach (var property in schema.Properties)
        {
            var field = CreateField(context, schema.GetTypeSchema(), property);
            type.Fields.Add(field);
        }

        foreach (var allOf in schema.AllOf)
        {
            foreach (var allOfProperty in allOf.Properties)
            {
                var field = CreateField(context, schema.GetTypeSchema(), allOfProperty);
                type.Fields.Add(field);
            }
        }

        if (!context.SkimmedSchema.Types.ContainsName(typeName))
        {
            context.SkimmedSchema.Types.Add(type);
        }

        return type;
    }

    public static OutputField CreateField(OpenApiWrapperContext context, OpenApiSchema schema, KeyValuePair<string, OpenApiSchema> property)
    {
        var isRequired = schema.Required.Contains(property.Key);
        var fieldType = property.Value.GetGraphQLTypeNode(isRequired);
        var field = new OutputField(OpenApiNamingHelper.GetFieldName(property.Key))
        {
            Type = fieldType,
            Description = property.Value.Description
        };

        CreateType(context, fieldType.NamedType().Name, property.Value);
        return field;
    }
}
