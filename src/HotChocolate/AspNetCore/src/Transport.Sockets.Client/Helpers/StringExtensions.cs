using System;

namespace HotChocolate.Transport.Sockets.Client.Helpers;

internal static class StringExtensions
{
    public static bool EqualsOrdinal(this string? s, string? other)
        => string.Equals(s, other, StringComparison.Ordinal);
}
