using System.Runtime.CompilerServices;
using static HotChocolate.Properties.PrimitivesResources;

namespace HotChocolate.Utilities;

/// <summary>
/// Helper methods to handle GraphQL names.
/// </summary>
public static class NameUtils
{
    private const byte _underscore = (byte)'_';

    /// <summary>
    /// Ensures that the name is a valid GraphQL type- or field-name.
    /// </summary>
    /// <param name="name">
    /// The name.
    /// </param>
    /// <param name="argumentName">
    /// The argument name.
    /// </param>
    /// <returns>
    /// Returns a string that represents a valid GraphQL type- or field-name.
    /// </returns>
    public static string EnsureGraphQLName(
        this string? name,
        [CallerArgumentExpression("name")]
        string argumentName = "name")
    {
        if (name.IsValidGraphQLName())
        {
            return name!;
        }

        throw new ArgumentException(NameUtils_InvalidGraphQLName, argumentName);
    }

    /// <summary>
    /// Checks if the provided name is a valid GraphQL type or field name.
    /// </summary>
    /// <param name="name">
    /// The name that shall be checked.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the name is a valid GraphQL name;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidGraphQLName(this string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var span = name.AsSpan();

        if (span[0].IsLetterOrUnderscore())
        {
            if (span.Length > 1)
            {
                for (var i = 1; i < span.Length; i++)
                {
                    if (!span[i].IsLetterOrDigitOrUnderscore())
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the provided name is a valid GraphQL type or field name.
    /// </summary>
    /// <param name="name">
    /// The name that shall be checked.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the name is a valid GraphQL name;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidGraphQLName(this in ReadOnlySpan<byte> name)
    {
        if (name.Length == 0)
        {
            return false;
        }

        if (name[0].IsLetterOrUnderscore())
        {
            if (name.Length > 1)
            {
                for (var i = 1; i < name.Length; i++)
                {
                    if (!name[i].IsLetterOrDigitOrUnderscore())
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Takes the provided name and replaces invalid
    /// characters with an underscore.
    /// </summary>
    /// <param name="name">
    /// A name that shall be made a value GraphQL name.
    /// </param>
    /// <returns>Returns a valid GraphQL name.</returns>
    public static string? MakeValidGraphQLName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var nameArray = name.ToCharArray();

        if (!nameArray[0].IsLetterOrUnderscore())
        {
            nameArray[0] = '_';
        }

        if (nameArray.Length > 1)
        {
            for (var i = 1; i < nameArray.Length; i++)
            {
                if (!nameArray[i].IsLetterOrDigitOrUnderscore())
                {
                    nameArray[i] = '_';
                }
            }
        }

        return new string(nameArray);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLetterOrDigitOrUnderscore(this char c)
        => IsLetterOrDigitOrUnderscore((byte)c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLetterOrDigitOrUnderscore(this byte c)
    {
        if (c is > 96 and < 123 or > 64 and < 91)
        {
            return true;
        }

        if (c is > 47 and < 58)
        {
            return true;
        }

        if (_underscore == c)
        {
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLetterOrUnderscore(this byte c)
    {
        if (c is > 96 and < 123 or > 64 and < 91)
        {
            return true;
        }

        if (_underscore == c)
        {
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLetterOrUnderscore(this char c)
        => IsLetterOrUnderscore((byte)c);
}
