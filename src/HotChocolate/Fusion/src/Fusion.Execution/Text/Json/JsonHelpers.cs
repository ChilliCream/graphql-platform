using System.Runtime.CompilerServices;

#if FUSION
namespace HotChocolate.Fusion.Text.Json;
#else
namespace HotChocolate.Text.Json;
#endif

internal static class JsonHelpers
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is between
    /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
        => (value - lowerBound) <= (upperBound - lowerBound);
}
