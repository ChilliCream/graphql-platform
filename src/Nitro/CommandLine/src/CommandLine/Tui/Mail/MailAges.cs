using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Formats a message's age relative to now for the list pane's age column.
/// </summary>
internal static class MailAges
{
    /// <summary>
    /// Formats the elapsed time between <paramref name="createdAt"/> and
    /// <paramref name="now"/> as a short relative label: "now" under a
    /// minute, then minutes, hours, or days, falling back to an ISO date
    /// once the message is a week or older. A non-positive elapsed time
    /// (a clock-skewed future timestamp) also formats as "now".
    /// </summary>
    public static string Format(DateTimeOffset createdAt, DateTimeOffset now)
    {
        var elapsed = now - createdAt;

        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "now";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return $"{(int)elapsed.TotalMinutes}m";
        }

        if (elapsed < TimeSpan.FromHours(24))
        {
            return $"{(int)elapsed.TotalHours}h";
        }

        if (elapsed < TimeSpan.FromDays(7))
        {
            return $"{(int)elapsed.TotalDays}d";
        }

        return createdAt.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
