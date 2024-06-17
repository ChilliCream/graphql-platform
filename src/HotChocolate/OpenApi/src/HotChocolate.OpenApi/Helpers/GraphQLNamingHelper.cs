using System.Text;
using System.Text.Json;
using HotChocolate.OpenApi.Extensions;
using HotChocolate.Types;

namespace HotChocolate.OpenApi.Helpers;

internal static class GraphQLNamingHelper
{
    public static string CreateInputTypeName(
        string mutationFieldName,
        MutationConventionOptions mutationConventionOptions)
    {
        var inputTypeNamePattern = mutationConventionOptions.InputTypeNamePattern ??
            MutationConventionOptionDefaults.InputTypeNamePattern;

        return inputTypeNamePattern.Replace(
            "{MutationName}",
            mutationFieldName.FirstCharacterToUpper());
    }

    public static string CreateName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(name));

        var stringBuilder = new StringBuilder();

        var lastCharacterInvalid = false;

        foreach (var character in name)
        {
            if (char.IsAsciiLetterOrDigit(character) || character == '_')
            {
                stringBuilder.Append(lastCharacterInvalid
                    ? char.ToUpperInvariant(character)
                    : character);

                lastCharacterInvalid = false;
            }
            else
            {
                lastCharacterInvalid = true;
            }
        }

        // The first character of a GraphQL name must be an ASCII letter or an underscore.
        // If not, prefix the name with an underscore to make it valid.
        if (!(char.IsAsciiLetter(stringBuilder[0]) || stringBuilder[0] == '_'))
        {
            stringBuilder.Insert(0, '_');
        }

        return stringBuilder.ToString();
    }

    public static string CreateObjectWrapperTypeName(Skimmed.ITypeDefinition type)
    {
        var typeName = JsonNamingPolicy.CamelCase.ConvertName(
            Skimmed.TypeExtensions.NamedType(type).Name).FirstCharacterToUpper();

        var suffix = Skimmed.TypeExtensions.IsListType(type) ? "List" : "";

        return $"{typeName}{suffix}Wrapper";
    }

    public static string CreateOperationResultName(string operationName)
    {
        return $"{operationName.FirstCharacterToUpper()}Result";
    }

    public static string CreatePayloadErrorTypeName(
        string mutationFieldName,
        MutationConventionOptions mutationConventionOptions)
    {
        var payloadErrorTypeNamePattern = mutationConventionOptions.PayloadErrorTypeNamePattern ??
            MutationConventionOptionDefaults.ErrorTypeNamePattern;

        return payloadErrorTypeNamePattern.Replace(
            "{MutationName}",
            mutationFieldName.FirstCharacterToUpper());
    }

    public static string CreatePayloadTypeName(
        string mutationFieldName,
        MutationConventionOptions mutationConventionOptions)
    {
        var payloadTypeNamePattern = mutationConventionOptions.PayloadTypeNamePattern ??
            MutationConventionOptionDefaults.PayloadTypeNamePattern;

        return payloadTypeNamePattern.Replace(
            "{MutationName}",
            mutationFieldName.FirstCharacterToUpper());
    }

    public static string CreateTypeName(string name)
    {
        return CreateName(name).FirstCharacterToUpper();
    }
}
