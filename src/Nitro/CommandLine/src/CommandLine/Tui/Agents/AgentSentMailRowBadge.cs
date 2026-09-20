using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Renders one sent-message row for the agent detail view's sent mail section as a single Spectre
/// markup line: age, subject, and recipients. Carries no selection prefix or unread marker.
/// </summary>
internal static class AgentSentMailRowBadge
{
    private const string NoRecipients = "-";
    private const string Arrow = "-> ";

    /// <summary>
    /// The maximum character budget for the recipients column.
    /// </summary>
    private const int MaxRecipientsBudget = 24;

    /// <summary>
    /// Builds the markup line for one sent-message row, truncating the
    /// subject and recipients with an ellipsis so the whole line fits within
    /// <paramref name="maxWidth"/> display columns. A
    /// <paramref name="maxWidth"/> of 0 or less produces an empty line.
    /// </summary>
    public static string Render(MailMessage message, DateTimeOffset now, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var age = MailAges.Format(message.CreatedAt, now);
        var recipients = string.Join(", ", message.Recipients.OrderBy(r => r.Ordinal).Select(r => r.Name));
        var recipientsText = recipients.Length == 0 ? NoRecipients : recipients;

        // Terminal-cell width of everything but the subject and recipients.
        var fixedPlainWidth = DisplayWidth.Measure(age) + 1 + DisplayWidth.Measure(Arrow);
        var remaining = Math.Max(0, maxWidth - fixedPlainWidth);
        var recipientsBudget = Math.Min(MaxRecipientsBudget, remaining / 2);
        var truncatedRecipients = DisplayWidth.Truncate(recipientsText, recipientsBudget);

        var subjectBudget = Math.Max(0, remaining - DisplayWidth.Measure(truncatedRecipients) - 1);
        var truncatedSubject = DisplayWidth.Truncate(message.Subject, subjectBudget);

        return
            $"{Markup.Escape(age)} {Markup.Escape(truncatedSubject)} {Arrow}{Markup.Escape(truncatedRecipients)}";
    }
}
