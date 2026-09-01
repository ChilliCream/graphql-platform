using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// Builds <see cref="MailMessage"/> and <see cref="MailRecipient"/>
/// instances with sensible defaults for agent detail model tests.
/// </summary>
internal static class MailMessageBuilder
{
    public static MailMessage Create(
        string id,
        string sender = "sender",
        string subject = "Subject",
        string body = "Body",
        DateTimeOffset? createdAt = null,
        IReadOnlyList<MailRecipient>? recipients = null)
        => new()
        {
            Id = id,
            ThreadId = id,
            Sender = sender,
            Subject = subject,
            Body = body,
            CreatedAt = createdAt ?? DateTimeOffset.UnixEpoch,
            Recipients = recipients ?? [ToRecipient("recipient")]
        };

    public static MailRecipient ToRecipient(string name, int ordinal = 0)
        => new() { Name = name, Kind = MailRecipientKinds.To, Ordinal = ordinal };
}
