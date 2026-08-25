using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// One durably-observed read of a <see cref="MailWakeReceipt"/>'s generation
/// against <c>mail_wake_batches</c>/<c>mail_wake_targets</c>: its echoed
/// <see cref="Actor"/> and <see cref="Generation"/>, its aggregate
/// <see cref="Status"/> (a <see cref="WakeReceiptAggregator"/> value), the
/// corresponding <see cref="WakeReceiptAggregator.IsZero"/> verdict, and
/// every target row this generation's batch reached. <see cref="Targets"/>
/// is empty both when the generation has not been claimed yet
/// (<see cref="Status"/> is <see cref="MailWakeTargetStatus.Pending"/>) and
/// when its batch found no live session to address
/// (<see cref="Status"/> is <see cref="MailWakeTargetStatus.Failed"/>).
/// </summary>
internal sealed record MailWakeObservation(
    string Actor,
    long Generation,
    string Status,
    bool IsZero,
    IReadOnlyList<ActorWakeTargetReceipt> Targets);

/// <summary>
/// Reads back a <see cref="MailWakeReceipt"/>'s durable dispatch state:
/// which batch (if any) has claimed its generation, and what every target
/// that batch reached has recorded so far. Read-only, no caching: each call
/// runs a fresh read transaction, so two calls observe whatever changed
/// between them.
/// </summary>
internal interface IMailWakeReceiptObserver
{
    Task<MailWakeObservation> ObserveAsync(MailWakeReceipt receipt, CancellationToken cancellationToken);
}
