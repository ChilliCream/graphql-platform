using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Renders one sent-message row for the agent detail view's sent mail
/// section as a single Spectre markup line: age, subject, and recipients.
/// Read-only: unlike <see cref="MailMessageBadge"/> there is no selection
/// prefix or unread marker, since this section has no drill-in.
/// </summary>
internal static class AgentSentMailRowBadge
{
    private const string Ellipsis = "…";
    private const string NoRecipients = "-";
    private const string Arrow = "-> ";

    /// <summary>
    /// The largest share of the available width the recipients list may
    /// claim, so a long recipient list cannot crowd the subject out
    /// entirely.
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

        // Plain-text length of everything but the subject and recipients, so
        // the remaining width can be split between them.
        var fixedPlainLength = age.Length + 1 + Arrow.Length;
        var remaining = Math.Max(0, maxWidth - fixedPlainLength);
        var recipientsBudget = Math.Min(MaxRecipientsBudget, remaining / 2);
        var truncatedRecipients = Truncate(recipientsText, recipientsBudget);

        var subjectBudget = Math.Max(0, remaining - truncatedRecipients.Length - 1);
        var truncatedSubject = Truncate(message.Subject, subjectBudget);

        return
            $"{Markup.Escape(age)} {Markup.Escape(truncatedSubject)} {Arrow}{Markup.Escape(truncatedRecipients)}";
    }

    private static string Truncate(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (value.Length <= width)
        {
            return value;
        }

        if (width == 1)
        {
            return Ellipsis;
        }

        return string.Concat(value.AsSpan(0, width - 1), Ellipsis);
    }
}
