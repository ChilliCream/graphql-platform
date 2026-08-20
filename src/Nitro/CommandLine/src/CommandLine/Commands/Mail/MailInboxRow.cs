using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

/// <summary>
/// The subset of a message's columns needed to print one inbox row.
/// </summary>
internal sealed class MailInboxRow
{
    public required string Id { get; init; }
    public required bool Unread { get; init; }
    public required string From { get; init; }
    public required string Subject { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset Now { get; init; }

    public string Format()
        => $"{Id}  {(Unread ? "*" : " ")}  {From}  {Subject}  {FormatAge(CreatedAt, Now)}";

    /// <summary>
    /// Formats the elapsed time between <paramref name="createdAt"/> and
    /// <paramref name="now"/> as a short relative label: "now" under a
    /// minute, then minutes, hours, or days, falling back to an ISO date once
    /// the message is a week or older. A non-positive elapsed time (a
    /// clock-skewed future timestamp) also formats as "now".
    /// </summary>
    private static string FormatAge(DateTimeOffset createdAt, DateTimeOffset now)
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
