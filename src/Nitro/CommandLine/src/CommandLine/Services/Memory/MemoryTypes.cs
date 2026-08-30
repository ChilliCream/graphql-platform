namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The well-known memory types. Custom normalized values are also accepted,
/// mirroring <c>TaskTypes</c>.
/// </summary>
internal static class MemoryTypes
{
    public const string Fact = "fact";
    public const string Decision = "decision";
    public const string Preference = "preference";
    public const string Reference = "reference";

    private const int MaxLength = 40;

    public static string Normalize(string type) => type.Trim().ToLowerInvariant();

    /// <summary>
    /// True when the normalized type is non-empty, at most 40 characters,
    /// and contains only lowercase ASCII letters, digits, and hyphens.
    /// </summary>
    public static bool IsValid(string normalizedType)
    {
        if (normalizedType.Length is 0 or > MaxLength)
        {
            return false;
        }

        foreach (var c in normalizedType)
        {
            if (c is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
            {
                return false;
            }
        }

        return true;
    }
}
