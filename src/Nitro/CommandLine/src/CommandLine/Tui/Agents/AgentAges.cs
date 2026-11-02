using System.Globalization;
using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Formats an agent's age fields (started, last seen, ended) for display, building on
/// <see cref="MailAges.Format"/>: a relative duration gets an " ago" suffix, a fresh
/// timestamp reads as "just now", and an absolute date is shown alone.
/// </summary>
internal static class AgentAges
{
    private const string NowLabel = "now";
    private const string JustNowLabel = "just now";
    private const string AgoSuffix = " ago";
    private const string IsoDateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Formats the elapsed time between <paramref name="value"/> and <paramref name="now"/>.
    /// "now" becomes "just now", a relative duration ("26m", "3h", "2d") gets an " ago"
    /// suffix, and an absolute date is returned as-is.
    /// </summary>
    public static string Format(DateTimeOffset value, DateTimeOffset now)
    {
        var formatted = MailAges.Format(value, now);

        if (formatted == NowLabel)
        {
            return JustNowLabel;
        }

        return IsAbsoluteDate(formatted) ? formatted : formatted + AgoSuffix;
    }

    private static bool IsAbsoluteDate(string formatted) =>
        DateTime.TryParseExact(
            formatted, IsoDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
