namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// Task priorities range from 0 (critical) to 4 (backlog); 2 is the default.
/// </summary>
internal static class TaskPriorities
{
    public const int Critical = 0;
    public const int High = 1;
    public const int Medium = 2;
    public const int Low = 3;
    public const int Backlog = 4;

    public static string Format(int priority) => $"P{priority}";

    /// <summary>
    /// Parses "0".."4" or "p0".."p4" (case-insensitive). Throws
    /// <see cref="ExitException"/> for any other value.
    /// </summary>
    public static int Parse(string value)
    {
        var span = value.AsSpan().Trim();

        if (span.Length > 1 && (span[0] is 'p' or 'P'))
        {
            span = span[1..];
        }

        if (span.Length == 1 && span[0] is >= '0' and <= '4')
        {
            return span[0] - '0';
        }

        throw new ExitException(
            $"Invalid priority '{value}'. Use 0-4 or p0-p4 (0 = critical, 4 = backlog).");
    }

    /// <summary>
    /// Parses a single priority or an inclusive range "N-M" / "pN-pM"
    /// (each bound accepted in the same forms as <see cref="Parse"/>). A
    /// single value returns a range with equal bounds. Throws
    /// <see cref="ExitException"/> when a bound is invalid or the low
    /// bound is greater than the high bound.
    /// </summary>
    public static (int Min, int Max) ParseRange(string value)
    {
        var trimmed = value.Trim();
        var dashIndex = trimmed.IndexOf('-');

        if (dashIndex > 0)
        {
            var min = Parse(trimmed[..dashIndex]);
            var max = Parse(trimmed[(dashIndex + 1)..]);

            if (min > max)
            {
                throw new ExitException(
                    $"Invalid priority range '{value}'. The low bound must be <= the high bound.");
            }

            return (min, max);
        }

        var single = Parse(trimmed);
        return (single, single);
    }
}
