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

    /// <summary>
    /// Whether the actor is this message's sender. Matches
    /// case-insensitively for the same reason <see cref="FindRecipient"/>
    /// does.
    /// </summary>
    public static bool IsFromActor(MailMessage message, string actor)
        => string.Equals(message.Sender, actor, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The other party or parties in the message, from the actor's point of
    /// view: the message's recipients when the actor sent it, or just its
    /// sender otherwise. Computed per message rather than per mailbox, so
    /// the mail board's PEER column renders identically wherever the
    /// message appears.
    /// </summary>
    public static IReadOnlyList<string> GetPeers(MailMessage message, string actor)
        => IsFromActor(message, actor)
            ? message.Recipients.Select(r => r.Name).ToArray()
            : [message.Sender];

    /// <summary>
    /// A one-character glyph summarizing the actor's relationship to the
    /// message, in the spirit of mutt's <c>$to_chars</c> reduced to what
    /// agents need: <see cref="FromActorGlyph"/> when the actor sent it,
    /// <see cref="DirectGlyph"/> when the actor is its sole recipient,
    /// <see cref="BroadcastGlyph"/> when the actor is one of several
    /// recipients, and <see cref="BlankGlyph"/> when the actor is neither
    /// party (foreign mail, for example between two other agents in the
    /// Workspace mailbox).
    /// </summary>
    public static char GetRelationshipGlyph(MailMessage message, string actor)
    {
        if (IsFromActor(message, actor))
        {
            return FromActorGlyph;
        }

        if (FindRecipient(message, actor) is null)
        {
            return BlankGlyph;
        }

        return message.Recipients.Count == 1 ? DirectGlyph : BroadcastGlyph;
    }

    /// <summary>The actor sent the message.</summary>
    public const char FromActorGlyph = 'F';

    /// <summary>The actor is the message's sole recipient.</summary>
    public const char DirectGlyph = '+';

    /// <summary>The actor is one of several recipients.</summary>
    public const char BroadcastGlyph = 'T';

    /// <summary>The actor is neither the sender nor a recipient.</summary>
    public const char BlankGlyph = ' ';
}
