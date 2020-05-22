using System;

namespace HotChocolate.Utilities
{
    public static class StringExtensions
    {
        public static bool EqualsOrdinal(this string? s, string? other) =>
            string.Equals(s, other, StringComparison.Ordinal);
    }
}
