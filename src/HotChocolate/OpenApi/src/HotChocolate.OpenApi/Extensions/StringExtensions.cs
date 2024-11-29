namespace HotChocolate.OpenApi.Extensions;

internal static class StringExtensions
{
    public static string FirstCharacterToLower(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length is 0)
        {
            return "";
        }

        var characters = value.ToCharArray();
        characters[0] = char.ToLowerInvariant(characters[0]);

        return new string(characters);
    }

    public static string FirstCharacterToUpper(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length is 0)
        {
            return "";
        }

        var characters = value.ToCharArray();
        characters[0] = char.ToUpperInvariant(characters[0]);

        return new string(characters);
    }
}
