using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

/// <summary>
/// Builds <see cref="MailMessage"/> and <see cref="MailRecipient"/>
/// instances with sensible defaults for mail board model tests.
/// </summary>
internal static class MailMessageBuilder
{
    public static MailMessage Create(
        string id,
        string sender = "sender",
        string subject = "Subject",
        string body = "Body",
        string? threadId = null,
        string? inReplyTo = null,
        DateTimeOffset? createdAt = null,
        IReadOnlyList<MailRecipient>? recipients = null)
        => new()
        {
            Id = id,
            ThreadId = threadId ?? id,
            InReplyTo = inReplyTo,
            Sender = sender,
            Subject = subject,
            Body = body,
            CreatedAt = createdAt ?? DateTimeOffset.UnixEpoch,
            Recipients = recipients ?? [ToRecipient("actor")]
        };

    public static MailRecipient ToRecipient(
        string name,
        int ordinal = 0,
        DateTimeOffset? readAt = null,
        DateTimeOffset? archivedAt = null)
        => new()
        {
            Name = name,
            Kind = MailRecipientKinds.To,
            Ordinal = ordinal,
            ReadAt = readAt,
            ArchivedAt = archivedAt
        };

    public static MailRecipient CcRecipient(
        string name,
        int ordinal = 0,
        DateTimeOffset? readAt = null,
        DateTimeOffset? archivedAt = null)
        => new()
        {
            Name = name,
            Kind = MailRecipientKinds.Cc,
            Ordinal = ordinal,
            ReadAt = readAt,
            ArchivedAt = archivedAt
        };
}
