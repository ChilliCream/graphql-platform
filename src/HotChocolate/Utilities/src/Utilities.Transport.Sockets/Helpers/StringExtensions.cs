namespace HotChocolate.Utilities.Transport.Sockets.Helpers;

internal static class StringExtensions
{
    public static bool EqualsOrdinal(this string? s, string? other)
        => string.Equals(s, other, StringComparison.Ordinal);
}
