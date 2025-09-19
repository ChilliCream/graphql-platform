using System.Runtime.CompilerServices;

namespace HotChocolate.Utilities;

public static class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsOrdinal(this string? s, string? other)
        => string.Equals(s, other, StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsInvariantIgnoreCase(this string? s, string? other)
        => string.Equals(s, other, StringComparison.InvariantCultureIgnoreCase);
}
