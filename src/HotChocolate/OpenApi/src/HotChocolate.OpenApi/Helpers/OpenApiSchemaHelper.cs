using System.Text;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.OpenApi.Models;
using IType = HotChocolate.Skimmed.IType;
using ListType = HotChocolate.Skimmed.ListType;
using NonNullType = HotChocolate.Skimmed.NonNullType;
using ObjectType = HotChocolate.Skimmed.ObjectType;

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

    public static IType GetGraphQLTypeNode(this OpenApiSchema schema, bool required)
    {
        var (name, format, isListType) = GetSchemaTypeInfo(schema);
        var graphqlName = GetGraphQLTypeName(name, format);
        var unwrappedType = new ObjectType(graphqlName);
        IType baseType = required
            ? new NonNullType(unwrappedType)
            : unwrappedType;

        return isListType
            ? new ListType(baseType)
            : baseType;
    }

    public static OpenApiSchema GetTypeSchema(this OpenApiSchema openApiSchema) =>
        openApiSchema.Items ?? openApiSchema;

    public static string RemoveWhiteSpacesAndEnsureName(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder();
        var capitalizeNext = false;

        // Go through all the characters
        foreach (var currentChar in input)
        {
            // Only process alphabetic characters and spaces
            if (!char.IsLetter(currentChar) && currentChar != ' ') continue;
            if (currentChar == ' ')
            {
                capitalizeNext = true; // We want to capitalize the next character
            }
            else if (capitalizeNext)
            {
                sb.Append(char.ToUpper(currentChar));
                capitalizeNext = false; // Reset flag after capitalizing
            }
            else
            {
                sb.Append(char.ToLower(currentChar));
            }
        }

        return NameUtils.MakeValidGraphQLName(sb.ToString()) ?? throw new InvalidOperationException("Field name can not be null");
    }

    public static (string possibleGraphQLName, bool isScalar) GetPossibleGraphQLTypeInfos(this OpenApiSchema schema)
    {
        var typeInfo = schema.GetSchemaTypeInfo();
        var possibleGraphQLName = OpenApiSchemaHelper.GetGraphQLTypeName(typeInfo.Name, typeInfo.Format);
        var isScalar = Scalars.IsBuiltIn(possibleGraphQLName);
        return (possibleGraphQLName, isScalar);
    }
}
