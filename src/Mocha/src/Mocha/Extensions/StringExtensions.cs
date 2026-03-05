using System.Runtime.CompilerServices;

namespace Mocha;

/// <summary>
/// Provides convenience string comparison extension methods with explicit comparison semantics.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Compares two strings using ordinal (case-sensitive, culture-invariant) comparison.
    /// </summary>
    /// <param name="s">The first string.</param>
    /// <param name="other">The second string.</param>
    /// <returns><c>true</c> if the strings are equal using ordinal comparison; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsOrdinal(this string? s, string? other)
        => string.Equals(s, other, StringComparison.Ordinal);

    /// <summary>
    /// Compares two strings using invariant culture, case-insensitive comparison.
    /// </summary>
    /// <param name="s">The first string.</param>
    /// <param name="other">The second string.</param>
    /// <returns><c>true</c> if the strings are equal ignoring case; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsInvariantIgnoreCase(this string? s, string? other)
        => string.Equals(s, other, StringComparison.InvariantCultureIgnoreCase);
}
