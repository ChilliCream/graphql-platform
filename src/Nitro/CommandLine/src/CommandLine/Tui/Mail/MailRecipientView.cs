using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Reads a <see cref="MailMessage"/>'s embedded recipients from one actor's
/// point of view: read and archived state are per recipient, not per
/// message.
/// </summary>
internal static class MailRecipientView
{
    /// <summary>
    /// Returns the given actor's recipient row on the message, or null when
    /// the actor is not a recipient (for example, the message's sender).
    /// Matches case-insensitively so an unnormalized actor still finds the
    /// store's normalized recipient name.
    /// </summary>
    public static MailRecipient? FindRecipient(MailMessage message, string actor)
    {
        foreach (var recipient in message.Recipients)
        {
            if (string.Equals(recipient.Name, actor, StringComparison.OrdinalIgnoreCase))
            {
                return recipient;
            }
        }

        return null;
    }

    /// <summary>
    /// Whether the actor has not yet read the message. False when the actor
    /// is not a recipient.
    /// </summary>
    public static bool IsUnread(MailMessage message, string actor)
        => FindRecipient(message, actor) is { ReadAt: null };

    /// <summary>
    /// Whether the actor has archived the message. False when the actor is
    /// not a recipient.
    /// </summary>
    public static bool IsArchived(MailMessage message, string actor)
        => FindRecipient(message, actor) is { ArchivedAt: not null };
}
