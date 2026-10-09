using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

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
    /// Formats elapsed time as minutes, hours, or days, or a UTC ISO date from one
    /// week onward. Returns "now" for activity less than a minute old or in the future.
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
