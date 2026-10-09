namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Normalizes and validates memory tags.
/// </summary>
internal static class MemoryTags
{
    private const int MaxLength = 40;

    public static string Normalize(string tag) => tag.Trim().ToLowerInvariant();

    /// <summary>
    /// True when the normalized tag is non-empty, at most 40 characters,
    /// and contains only lowercase ASCII letters, digits, and hyphens.
    /// </summary>
    public static bool IsValid(string normalizedTag)
    {
        if (normalizedTag.Length is 0 or > MaxLength)
        {
            return false;
        }

        foreach (var c in normalizedTag)
        {
            if (c is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
            {
                return false;
            }
        }

        return true;
    }
}
