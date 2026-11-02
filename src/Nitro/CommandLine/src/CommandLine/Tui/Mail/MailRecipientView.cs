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
    /// Returns the actor's recipient row using a case-insensitive name match,
    /// or null when the actor is not a recipient.
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
    /// Whether the actor is the message's sender, compared case-insensitively.
    /// </summary>
    public static bool IsFromActor(MailMessage message, string actor)
        => string.Equals(message.Sender, actor, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns all recipient names when the actor sent the message, or the sender
    /// name otherwise.
    /// </summary>
    public static IReadOnlyList<string> GetPeers(MailMessage message, string actor)
        => IsFromActor(message, actor)
            ? message.Recipients.Select(r => r.Name).ToArray()
            : [message.Sender];

    /// <summary>
    /// A one-character glyph summarizing the actor's relationship to the
    /// message: <see cref="FromActorGlyph"/> when the actor sent it,
    /// <see cref="DirectGlyph"/> when the actor is its sole recipient,
    /// <see cref="BroadcastGlyph"/> when the actor is one of several
    /// recipients, and <see cref="BlankGlyph"/> when the actor is neither party.
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
