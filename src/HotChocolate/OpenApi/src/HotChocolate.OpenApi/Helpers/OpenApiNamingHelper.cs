using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.OpenApi.Helpers;

internal static class OpenApiNamingHelper
{
    public static string GetFieldName(string value) => value
                                                           .RemoveWhiteSpacesAndEnsureName()
                                                           .EnsureStartWithLowerChar() ??
                                                throw new InvalidOperationException("Field name cannot be null");

    public static string GetTypeName(string value) => value
                                                          .RemoveWhiteSpacesAndEnsureName()
                                                          .EnsureStartWithUpperChar() ??
                                                       throw new InvalidOperationException("Field name cannot be null");

    public static string GetInputTypeName(string value) => $"{GetTypeName(value)}InputType";

    private static string EnsureStartWithLowerChar(this string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        return char.ToLowerInvariant(text[0]) + text[1..];
    }

    private static string EnsureStartWithUpperChar(this string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    private static string RemoveWhiteSpacesAndEnsureName(this string input)
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
                sb.Append(currentChar);
            }
        }

        return NameUtils.MakeValidGraphQLName(sb.ToString()) ?? throw new InvalidOperationException("Field name can not be null");
    }
}
