using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

/// <summary>
/// Runs the direct-first actor-wake state machine for every recipient a
/// send, reply, or broadcast just committed, and turns its durable outcome
/// into a <see cref="MailNotificationResult"/>. Shared by every mail command
/// so <c>send</c>, <c>reply</c>, and <c>broadcast</c> report identically. The
/// a mixed multi-recipient outcome never reports as clean regardless of
/// recipient order.
/// </summary>
internal static class MailWakeDispatch
{
    /// <summary>
    /// Dispatches every recipient's <see cref="MailMessage.WakeReceipts"/> through
    /// <paramref name="dispatcher"/> under one shared deadline fixed once for
    /// this call, then reads back each receipt's durable outcome through
    /// <paramref name="observer"/>, passed the same shared deadline, so the
    /// result reflects this message's own generation even when the dispatch
    /// attempt itself lost its claim to another owner, and an observer that
    /// retries until settlement never computes its own fresh deadline per
    /// recipient, which would otherwise hold later recipients' slots for a
    /// multiple of this call's own deadline.
    /// </summary>
    public static async Task<MailNotificationResult> RunAsync(
        MailMessage message,
        IActorWakeDispatcher dispatcher,
        IMailWakeReceiptObserver observer,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var deadline = timeProvider.GetUtcNow() + WakeDispatchPolicy.BatchDeadline;
        var recipients = new List<MailWakeRecipientResult>(message.WakeReceipts.Count);
        var observations = new List<MailWakeObservation>(message.WakeReceipts.Count);

        foreach (var receipt in message.WakeReceipts)
        {
            await dispatcher.DispatchAsync(receipt.Actor, deadline, cancellationToken);
            var observation = await observer.ObserveAsync(receipt, deadline, cancellationToken);
            observations.Add(observation);
            recipients.Add(MailWakeRecipientResult.Create(receipt, observation));
        }

        var recipientStatuses = observations.Select(o => o.Status).ToList();
        var status = WakeReceiptAggregator.Aggregate(recipientStatuses);
        var deliveryPending = observations.Any(observation =>
            observation.Status is MailWakeTargetStatus.Pending or MailWakeTargetStatus.Delegated
            || observation.Targets.Any(target => target.Status is MailWakeTargetStatus.Pending or MailWakeTargetStatus.Delegated));

        return new MailNotificationResult
        {
            Status = status,
            DeliveryPending = deliveryPending,
            Recipients = recipients
        };
    }
}
