using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.OpenApi.Helpers;

internal static class OpenApiNamingHelper
{
    public static string GetFieldName(string value)
    {
        var sb = new StringBuilder(value.Length);
        var toUpper = false;
        var alreadyCamelCase = true;

        foreach (var c in value.AsSpan())
        {
            if (char.IsLetterOrDigit(c))
            {
                if (toUpper)
                {
                    sb.Append(char.ToUpperInvariant(c));
                    toUpper = false;
                }
                else
                {
                    sb.Append(c);
                }

            }
            else if (c is '_' or '-' or ' ')
            {
                toUpper = true;
                alreadyCamelCase = false;
            }
        }

        if (sb.Length > 0 && char.IsUpper(sb[0]))
        {
            sb[0] = char.ToLowerInvariant(sb[0]);
            alreadyCamelCase = false;
        }

        return alreadyCamelCase ? value : sb.ToString();
    }

    public static string GetInputTypeName(string value) 
        => $"{GetTypeName(value)}Input";
    
    public static string GetPayloadTypeName(string value) 
        => $"{GetTypeName(value)}Payload";

    public static string GetTypeName(string value) 
        => value.RemoveCharacterAndEnsureName(' ').EnsureStartWithUpperChar() ??
            throw new OpenApiFieldNameNullException();

    public static string GetPathAsName(string path)
        => path.RemoveCharacterAndEnsureName('/').EnsureStartWithUpperChar();

    private static string EnsureStartWithLowerChar(this string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return char.ToLowerInvariant(text[0]) + text[1..];
    }

    private static string EnsureStartWithUpperChar(this string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    private static string RemoveCharacterAndEnsureName(this string input, char c)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var capitalizeNext = false;

        // Go through all the characters
        foreach (var currentChar in input.AsSpan())
        {
            // Only process alphabetic characters and spaces
            if (!char.IsLetter(currentChar) && currentChar != c)
            {
                continue;
            }
            
            if (currentChar == c)
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

        return NameUtils.MakeValidGraphQLName(sb.ToString()) ?? throw new OpenApiFieldNameNullException();
    }
}
