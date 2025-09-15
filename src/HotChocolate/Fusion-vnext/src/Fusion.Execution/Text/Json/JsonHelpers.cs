using System.Runtime.CompilerServices;

namespace HotChocolate.Text.Json;

internal static class JsonHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidDateTimeOffsetParseLength(int length)
    {
        return IsInRangeInclusive(
            length,
            JsonConstants.MinimumDateTimeParseLength,
            JsonConstants.MaximumEscapedDateTimeOffsetParseLength);
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is between
    /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInRangeInclusive(int value, int lowerBound, int upperBound)
        => (uint)(value - lowerBound) <= (uint)(upperBound - lowerBound);
}
