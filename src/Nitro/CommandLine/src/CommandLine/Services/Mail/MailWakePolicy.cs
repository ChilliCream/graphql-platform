namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Whether sending or replying to a message advances a recipient's
/// actor-wake generation. <see cref="Skip"/> stores the message without
/// touching wake intent for any recipient, and is the default for callers
/// not yet migrated to select a policy. <see cref="Enqueue"/> increments
/// <c>mail_wake_outbox.requested_generation</c> once per distinct recipient,
/// in the same transaction as the message and recipient rows.
/// </summary>
internal enum MailWakePolicy
{
    Skip,
    Enqueue
}
