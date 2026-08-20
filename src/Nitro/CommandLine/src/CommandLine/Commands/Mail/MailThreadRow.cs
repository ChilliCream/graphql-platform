using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

/// <summary>
/// The subset of a thread's columns needed to print one threads row.
/// </summary>
internal sealed class MailThreadRow
{
    public required string ThreadId { get; init; }
    public required string Subject { get; init; }
    public required IReadOnlyList<string> Participants { get; init; }
    public required int MessageCount { get; init; }
    public required int UnreadCount { get; init; }
    public required DateTimeOffset LastActivityAt { get; init; }
    public required DateTimeOffset Now { get; init; }

    public string Format()
        => $"{ThreadId}  {(UnreadCount > 0 ? "*" : " ")}  {Subject}  "
            + $"{string.Join(",", Participants)}  {MessageCount}  {UnreadCount}  "
            + FormatAge(LastActivityAt, Now);

    /// <summary>
    /// Formats the elapsed time between <paramref name="lastActivityAt"/> and
    /// <paramref name="now"/> as a short relative label: "now" under a
    /// minute, then minutes, hours, or days, falling back to an ISO date once
    /// the thread's last activity is a week or older. A non-positive elapsed
    /// time (a clock-skewed future timestamp) also formats as "now".
    /// </summary>
    private static string FormatAge(DateTimeOffset lastActivityAt, DateTimeOffset now)
    {
        var elapsed = now - lastActivityAt;

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

        return lastActivityAt.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
