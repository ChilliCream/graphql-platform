using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Renders one message row for the mail board's list pane as a single
/// Spectre markup line: selection prefix, unread marker, relationship
/// glyph, peer, subject, and age.
/// </summary>
internal static class MailMessageBadge
{
    private const string Ellipsis = "…";
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string UnreadMarker = "*";
    private const string ReadMarker = " ";
    private const string ToPrefix = "To ";

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
        var glyph = MailRecipientView.GetRelationshipGlyph(message, actor);
        var peer = FormatPeer(message, actor);
        var age = MailAges.Format(message.CreatedAt, now);

        // Plain-text length of everything but the subject, so the subject
        // can be truncated to make the whole line fit maxWidth.
        var fixedPlainLength = prefix.Length + marker.Length + 1
            + 1 + 1
            + peer.Length + 1
            + age.Length + 1;

        var subjectBudget = Math.Max(0, maxWidth - fixedPlainLength);
        var truncatedSubject = Truncate(message.Subject, subjectBudget);
        var escapedSubject = Markup.Escape(truncatedSubject);
        var subjectMarkup = unread
            ? Stylize(ThemeTokens.GetStyle("mail.message.unread").ToMarkup(), escapedSubject)
            : escapedSubject;

        var line =
            $"{Markup.Escape(prefix)}{Markup.Escape(marker)} "
            + $"{Markup.Escape(glyph.ToString())} "
            + $"{Markup.Escape(peer)} "
            + $"{subjectMarkup} "
            + $"{Markup.Escape(age)}";

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = Stylize(highlightStyle, line);
        }

        return line;
    }

    /// <summary>
    /// The PEER column text for a message, from the actor's point of view:
    /// the sender when the actor received the message, or a
    /// <c>"To "</c>-prefixed summary of the recipients when the actor sent
    /// it. Computed per message via <see cref="MailRecipientView.GetPeers"/>,
    /// never per mailbox, so it renders identically wherever the message
    /// appears. Multiple recipients show the first plus a <c>+N</c>
    /// overflow count rather than a truncated list.
    /// </summary>
    private static string FormatPeer(MailMessage message, string actor)
    {
        var peers = MailRecipientView.GetPeers(message, actor);
        if (!MailRecipientView.IsFromActor(message, actor))
        {
            return peers[0];
        }

        if (peers.Count == 0)
        {
            return ToPrefix.TrimEnd();
        }

        var overflow = peers.Count - 1;
        var overflowSuffix = overflow > 0 ? $"+{overflow}" : string.Empty;
        return ToPrefix + peers[0] + overflowSuffix;
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
