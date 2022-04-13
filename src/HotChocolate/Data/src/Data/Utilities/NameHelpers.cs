using System;
using System.Globalization;

namespace HotChocolate.Data.Utilities;

internal static class NameHelpers
{
    public static string UppercaseFirstLetter(string? s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        s = s.Trim();
        
        if (s.Length < 1)
        {
            throw new ArgumentException("Provided string was empty.", nameof(s));
        }

        return $"{char.ToUpper(s[0], CultureInfo.InvariantCulture)}{s.Substring(1)}";
    }
}
