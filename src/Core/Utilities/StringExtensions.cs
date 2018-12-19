using System;

namespace HotChocolate.Utilities
{
    public static class StringExtensions
    {
        public static bool EqualsOrdinal(this string s, string other)
        {
            return string.Equals(s, other, StringComparison.Ordinal);
        }
    }
}
