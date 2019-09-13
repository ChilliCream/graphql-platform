using System;

namespace StrawberryShake.Generators.Utilities
{
    internal static class StringExtensions
    {
        public static bool EqualsOrdinal(this string a, string b)
        {
            return string.Equals(a, b, StringComparison.Ordinal);
        }
    }
}
