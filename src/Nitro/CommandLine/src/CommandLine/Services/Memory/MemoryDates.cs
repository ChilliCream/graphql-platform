using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Formats the UTC RFC 3339 timestamps stored in memory frontmatter.
/// </summary>
internal static class MemoryDates
{
    public static string Format(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
}
