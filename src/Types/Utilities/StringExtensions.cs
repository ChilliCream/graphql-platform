using System;

namespace HotChocolate.Utilities
{
    internal static class StringExtensions
    {
        public static bool EqualsOrdinal(this string x, string y)
        {
            return string.Equals(x, y, StringComparison.Ordinal);
        }
    }
}
