using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

/// <summary>
/// The full actor-wake notification outcome for a sent or replied message:
/// the aggregate <see cref="Status"/> across every recipient (the same
/// lattice <see cref="WakeReceiptAggregator.Aggregate"/> derives target
/// status from), whether delivery remains outstanding for any recipient, and
/// every recipient's own outcome in deterministic order.
/// </summary>
internal sealed record MailNotificationResult
{
    public required string Status { get; init; }
    public required bool DeliveryPending { get; init; }
    public required IReadOnlyList<MailWakeRecipientResult> Recipients { get; init; }
}
