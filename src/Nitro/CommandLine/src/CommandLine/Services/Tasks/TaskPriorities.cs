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
}
