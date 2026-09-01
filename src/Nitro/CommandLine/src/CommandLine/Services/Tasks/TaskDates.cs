using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// Parses and formats the timestamps used by task commands. Values without an
/// offset are interpreted as UTC.
/// </summary>
internal static class TaskDates
{
    public static DateTimeOffset Parse(string value, string optionName)
    {
        if (DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var result))
        {
            // Stored timestamps must all carry the +00:00 offset so SQLite's
            // lexicographic TEXT comparison stays chronologically correct.
            return result.ToUniversalTime();
        }

        throw new ExitException(
            $"Invalid date '{value}' for '{optionName}'. "
            + "Use an ISO 8601 date or timestamp.");
    }

    public static string Format(DateTimeOffset value)
        => value.ToUniversalTime()
            .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
}
