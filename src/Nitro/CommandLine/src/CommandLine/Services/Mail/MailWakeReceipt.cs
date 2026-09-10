namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// One recipient's actor-wake generation token committed in the same
/// transaction as a sent or replied message: the recipient's
/// <c>mail_wake_outbox.requested_generation</c> after this message, on the
/// local Nitro instance. Represents unread mail through this generation; it
/// does not copy the message body, and it is not a promise that this one
/// message appears in any single bounded digest.
/// </summary>
internal sealed record MailWakeReceipt
{
    public required string Actor { get; init; }
    public required long Generation { get; init; }
}
