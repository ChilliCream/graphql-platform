using System.Runtime.CompilerServices;

namespace HotChocolate.Fusion.Text.Json;

internal static class JsonHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidDateTimeOffsetParseLength(int length)
        => IsInRangeInclusive(
            length,
            JsonConstants.MinimumDateTimeParseLength,
            JsonConstants.MaximumEscapedDateTimeOffsetParseLength);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is between
    /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
        => (value - lowerBound) <= (upperBound - lowerBound);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is between
    /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInRangeInclusive(int value, int lowerBound, int upperBound)
        => (uint)(value - lowerBound) <= (uint)(upperBound - lowerBound);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidUnescapedDateTimeOffsetParseLength(int length)
        => IsInRangeInclusive(
            length,
            JsonConstants.MinimumDateTimeParseLength,
            JsonConstants.MaximumDateTimeOffsetParseLength);
}

public ref struct Foo
{
    public readonly List<string> s = [];

    public Foo()
    {
    }
}
