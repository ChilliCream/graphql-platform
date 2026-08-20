using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Renders one message row for the mail board's list pane as a single
/// Spectre markup line: selection prefix, unread marker, sender, subject,
/// and age.
/// </summary>
internal static class MailMessageBadge
{
    private const string Ellipsis = "…";
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string UnreadMarker = "*";
    private const string ReadMarker = " ";

    /// <summary>
    /// Builds the markup line for one message row, truncating the subject
    /// with an ellipsis so the whole line fits within
    /// <paramref name="maxWidth"/> display columns. A
    /// <paramref name="maxWidth"/> of 0 or less produces an empty line.
    /// </summary>
    public static string Render(
        MailMessage message,
        string actor,
        DateTimeOffset now,
        bool selected,
        int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var unread = MailRecipientView.IsUnread(message, actor);
        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var marker = unread ? UnreadMarker : ReadMarker;
        var age = MailAges.Format(message.CreatedAt, now);

        // Plain-text length of everything but the subject, so the subject
        // can be truncated to make the whole line fit maxWidth.
        var fixedPlainLength = prefix.Length + marker.Length + 1
            + message.Sender.Length + 1
            + age.Length + 1;

        var subjectBudget = Math.Max(0, maxWidth - fixedPlainLength);
        var truncatedSubject = Truncate(message.Subject, subjectBudget);
        var escapedSubject = Markup.Escape(truncatedSubject);
        var subjectMarkup = unread
            ? Stylize(ThemeTokens.GetStyle("mail.message.unread").ToMarkup(), escapedSubject)
            : escapedSubject;

        var line =
            $"{Markup.Escape(prefix)}{Markup.Escape(marker)} "
            + $"{Markup.Escape(message.Sender)} "
            + $"{subjectMarkup} "
            + $"{Markup.Escape(age)}";

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = Stylize(highlightStyle, line);
        }

        return line;
    }

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";

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
