namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// The parameters for <see cref="IMailStore.SendMessageAsync"/>. Recipient
/// names are normalized, deduped, and validated by the store; they need not
/// be normalized by the caller.
/// </summary>
internal sealed record MailMessageCreation
{
    public required string Sender { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public IReadOnlyList<string> To { get; init; } = [];
    public IReadOnlyList<string> Cc { get; init; } = [];

    /// <summary>
    /// Whether this send advances every recipient's actor-wake generation.
    /// Defaults to <see cref="MailWakePolicy.Skip"/> for callers not yet
    /// migrated to select a policy; a caller that wants recipients woken
    /// must set this to <see cref="MailWakePolicy.Enqueue"/> explicitly.
    /// </summary>
    public MailWakePolicy WakePolicy { get; init; } = MailWakePolicy.Skip;
}
