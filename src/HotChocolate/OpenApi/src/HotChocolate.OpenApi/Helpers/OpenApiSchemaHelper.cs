using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi.Helpers;

internal static class OpenApiSchemaHelper
{
    public  static (string Name, string? Format, bool IsListType) GetSchemaTypeInfo(this OpenApiSchema schema)
    {
        var isList = schema.Items is not null;

        var name = isList ? schema.Items!.Type : schema.Type;
        var format = isList ? schema.Items!.Format : schema.Format;

        name ??= isList ? schema.Items!.Reference.Id : schema.Reference.Id;

        return (name, format, isList);
    }

    public static string GetGraphQLTypeName(string openApiSchemaTypeName, string? format)
    {
        var typename = openApiSchemaTypeName switch
        {
            "string" => ScalarNames.String,
            "integer" => format == "int64" ? ScalarNames.Long : ScalarNames.Int,
            "boolean" => ScalarNames.Boolean,
            _ => NameUtils.MakeValidGraphQLName(openApiSchemaTypeName)
        };
        return typename ?? throw new InvalidOperationException();
    }

    public static ITypeNode GetGraphQLTypeNode(this OpenApiSchema schema, bool required)
    {
        var (name, format, isListType) = GetSchemaTypeInfo(schema);
        var graphqlName = GetGraphQLTypeName(name, format);
        ITypeNode baseType = required
            ? new NonNullTypeNode(new NamedTypeNode(graphqlName))
            : new NamedTypeNode(graphqlName);

        return isListType
            ? new ListTypeNode(baseType)
            : baseType;
    }
}
